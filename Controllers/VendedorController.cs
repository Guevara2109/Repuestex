using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Repuestex.Data;
using Repuestex.Models;
using Repuestex.ViewModels;

namespace Repuestex.Controllers;

/// <summary>
/// Panel del vendedor: crea, edita y elimina (baja logica) sus propios productos.
/// Solo el rol Vendedor entra (RN-12); cada accion vuelve a comprobar que el producto sea suyo.
/// </summary>
[Authorize(Roles = "Vendedor")]
public class VendedorController : Controller
{
    private readonly VendedorRepository _vendedor;
    private readonly CatalogoRepository _catalogo;
    private readonly ILogger<VendedorController> _log;

    public VendedorController(VendedorRepository vendedor, CatalogoRepository catalogo, ILogger<VendedorController> log)
    {
        _vendedor = vendedor;
        _catalogo = catalogo;
        _log = log;
    }

    // El id del usuario y de su proveedor salen de la cookie de sesion, nunca del formulario.
    private bool TryContexto(out int idUsuario, out int idProveedor)
    {
        idUsuario = 0;
        idProveedor = 0;
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out idUsuario)
            && int.TryParse(User.FindFirstValue("IdProveedor"), out idProveedor);
    }

    // ---------- Lista ----------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryContexto(out _, out var idProveedor)) return Forbid();
        try
        {
            return View(await _vendedor.ListarAsync(idProveedor));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al listar los productos del vendedor");
            TempData["Error"] = "No se pudieron cargar tus productos. Intenta de nuevo más tarde.";
            return View(new List<RepuestoVendedor>());
        }
    }

    // ---------- Crear ----------
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        var m = new RepuestoFormViewModel();
        await CargarListasAsync(m);
        return View("Formulario", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RepuestoFormViewModel m)
    {
        if (!TryContexto(out var idUsuario, out var idProveedor)) return Forbid();

        m.IdRepuesto = 0;
        Normalizar(m);
        await ValidarAsync(m);

        if (ModelState.IsValid)
        {
            try
            {
                await _vendedor.CrearAsync(m, idUsuario, idProveedor);
                TempData["Mensaje"] = "Producto creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)   // numero de parte duplicado (carrera)
            {
                ModelState.AddModelError(nameof(m.NumeroParte), "Ese número de parte ya existe en el sistema.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al crear el producto");
                ModelState.AddModelError("", "No se pudo guardar el producto. Intenta de nuevo más tarde.");
            }
        }

        await CargarListasAsync(m);
        return View("Formulario", m);
    }

    // ---------- Editar ----------
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        if (!TryContexto(out _, out var idProveedor)) return Forbid();

        var m = await _vendedor.ObtenerAsync(id, idProveedor);
        if (m is null) return NotFound();   // no existe, esta dada de baja o es de otro vendedor

        await CargarListasAsync(m);
        return View("Formulario", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, RepuestoFormViewModel m)
    {
        if (!TryContexto(out _, out var idProveedor)) return Forbid();

        m.IdRepuesto = id;
        Normalizar(m);
        await ValidarAsync(m);

        if (ModelState.IsValid)
        {
            try
            {
                if (!await _vendedor.ActualizarAsync(m, idProveedor)) return NotFound();
                TempData["Mensaje"] = "Producto actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
            {
                ModelState.AddModelError(nameof(m.NumeroParte), "Ese número de parte ya existe en el sistema.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al actualizar el producto {Id}", id);
                ModelState.AddModelError("", "No se pudo guardar el producto. Intenta de nuevo más tarde.");
            }
        }

        await CargarListasAsync(m);
        return View("Formulario", m);
    }

    // ---------- Eliminar (baja logica) ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (!TryContexto(out _, out var idProveedor)) return Forbid();
        try
        {
            if (await _vendedor.EliminarAsync(id, idProveedor))
                TempData["Mensaje"] = "Producto eliminado. Ya no aparece en las búsquedas.";
            else
                TempData["Error"] = "No se encontró el producto o no te pertenece.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al eliminar el producto {Id}", id);
            TempData["Error"] = "No se pudo eliminar el producto. Intenta de nuevo más tarde.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- Apoyo ----------
    private static void Normalizar(RepuestoFormViewModel m)
    {
        m.NumeroParte = (m.NumeroParte ?? "").Trim();
        m.Nombre = (m.Nombre ?? "").Trim();
        m.Descripcion = string.IsNullOrWhiteSpace(m.Descripcion) ? null : m.Descripcion.Trim();
        m.ImagenUrl = string.IsNullOrWhiteSpace(m.ImagenUrl) ? null : m.ImagenUrl.Trim();
        m.NumerosAlternos = string.IsNullOrWhiteSpace(m.NumerosAlternos) ? null : m.NumerosAlternos.Trim();
    }

    private async Task ValidarAsync(RepuestoFormViewModel m)
    {
        if (m.Tipo is not ("Original" or "Alterno"))
            ModelState.AddModelError(nameof(m.Tipo), "Elige Original o Alterno.");

        if (m.ImagenUrl is not null &&
            !(Uri.TryCreate(m.ImagenUrl, UriKind.Absolute, out var uri) &&
              (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            ModelState.AddModelError(nameof(m.ImagenUrl), "Escribe una dirección web válida (http o https).");

        if (m.ObtenerAlternos().Any(a => a.Length > 40))
            ModelState.AddModelError(nameof(m.NumerosAlternos), "Cada número alterno puede tener hasta 40 caracteres.");

        // RN-03: al menos una compatibilidad, y todos los vehiculos deben existir
        m.IdsVehiculos = m.IdsVehiculos.Distinct().ToList();
        if (m.IdsVehiculos.Count == 0)
        {
            ModelState.AddModelError(nameof(m.IdsVehiculos), "Agrega al menos un vehículo compatible.");
        }
        else
        {
            var existentes = await _vendedor.ObtenerVehiculosAsync(m.IdsVehiculos);
            if (existentes.Count != m.IdsVehiculos.Count)
                ModelState.AddModelError(nameof(m.IdsVehiculos), "Algún vehículo elegido no existe.");
        }

        if (m.IdCategoria is int idCategoria)
        {
            var categorias = await _catalogo.ObtenerCategoriasAsync();
            if (categorias.All(c => c.IdCategoria != idCategoria))
                ModelState.AddModelError(nameof(m.IdCategoria), "La categoría elegida no existe.");
        }

        // RN-04: numero de parte unico
        if (m.NumeroParte.Length > 0 && await _vendedor.ExisteNumeroParteAsync(m.NumeroParte, m.IdRepuesto))
            ModelState.AddModelError(nameof(m.NumeroParte), "Ese número de parte ya existe en el sistema.");
    }

    private async Task CargarListasAsync(RepuestoFormViewModel m)
    {
        m.Categorias = await _catalogo.ObtenerCategoriasAsync();
        m.Marcas = await _catalogo.ObtenerMarcasAsync();
        m.VehiculosSeleccionados = await _vendedor.ObtenerVehiculosAsync(m.IdsVehiculos);
    }
}
