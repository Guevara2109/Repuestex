using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Repuestex.Data;
using Repuestex.ViewModels;

namespace Repuestex.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly AdminRepository _admin;
    private readonly CatalogoRepository _catalogo;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AdminRepository admin, CatalogoRepository catalogo, ILogger<AdminController> logger)
    {
        _admin = admin;
        _catalogo = catalogo;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Repuestos(string? buscar)
    {
        ViewBag.Buscar = buscar;
        return View(await _admin.ListarAsync(buscar));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo = await _admin.ObtenerAsync(id);
        if (modelo is null) return NotFound();
        modelo.Categorias = await _catalogo.ObtenerCategoriasAsync();
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, AdminRepuestoFormViewModel modelo)
    {
        if (id <= 0) return BadRequest();
        modelo.IdRepuesto = id;
        modelo.NumeroParte = (modelo.NumeroParte ?? "").Trim();
        modelo.Nombre = (modelo.Nombre ?? "").Trim();
        modelo.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim();
        modelo.ImagenUrl = string.IsNullOrWhiteSpace(modelo.ImagenUrl) ? null : modelo.ImagenUrl.Trim();

        if (modelo.Tipo is not ("Original" or "Alterno"))
            ModelState.AddModelError(nameof(modelo.Tipo), "Tipo inválido.");

        if (modelo.ImagenUrl is not null &&
            (!Uri.TryCreate(modelo.ImagenUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            ModelState.AddModelError(nameof(modelo.ImagenUrl), "La URL debe comenzar con http o https.");

        modelo.Categorias = await _catalogo.ObtenerCategoriasAsync();
        if (modelo.IdCategoria is not int cat || modelo.Categorias.All(c => c.IdCategoria != cat))
            ModelState.AddModelError(nameof(modelo.IdCategoria), "Categoría inexistente.");

        if (!string.IsNullOrWhiteSpace(modelo.NumeroParte) &&
            await _admin.NumeroParteOcupadoAsync(modelo.NumeroParte, id))
            ModelState.AddModelError(nameof(modelo.NumeroParte), "El número de parte ya existe.");

        if (!ModelState.IsValid) return View(modelo);
        try
        {
            if (!await _admin.ActualizarAsync(modelo)) return NotFound();
            TempData["Mensaje"] = "Repuesto actualizado correctamente.";
            return RedirectToAction(nameof(Repuestos));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(nameof(modelo.NumeroParte), "El número de parte ya existe.");
            return View(modelo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar repuesto {Id}", id);
            ModelState.AddModelError("", "No fue posible guardar los cambios.");
            return View(modelo);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        try
        {
            TempData["Mensaje"] = await _admin.CambiarEstadoAsync(id, activo)
                ? activo ? "Repuesto reactivado." : "Repuesto desactivado."
                : "No se realizó ningún cambio.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado de repuesto {Id}", id);
            TempData["Error"] = "No fue posible cambiar el estado.";
        }
        return RedirectToAction(nameof(Repuestos));
    }
}
