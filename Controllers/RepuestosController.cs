using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Repuestex.Data;
using Repuestex.ViewModels;

namespace Repuestex.Controllers;

public class RepuestosController : Controller
{
    private const int TamPagina = 10;

    private readonly CatalogoRepository _catalogo;
    private readonly ILogger<RepuestosController> _log;

    public RepuestosController(CatalogoRepository catalogo, ILogger<RepuestosController> log)
    {
        _catalogo = catalogo;
        _log = log;
    }

    // ---------- Resultados (CU-03, CU-04, RF-04, RF-05, RF-06, RF-11) ----------
    [HttpGet]
    public async Task<IActionResult> Buscar(FiltroBusqueda f)
    {
        // Validaciones (RN-01)
        if (f.Anio is not null && (f.Anio < 2008 || f.Anio > 2026))
        {
            TempData["Error"] = "El año debe estar entre 2008 y 2026.";
            return RedirectToAction("Index", "Home");
        }
        if (!f.TieneCriterio)
        {
            TempData["Error"] = "Selecciona un vehículo o escribe el nombre o número de parte que buscas.";
            return RedirectToAction("Index", "Home");
        }
        if (f.Pagina < 1) f.Pagina = 1;
        if (f.Tipo is not ("Original" or "Alterno")) f.Tipo = null;

        try
        {
            var categorias = await _catalogo.ObtenerCategoriasAsync();
            var titulo = await _catalogo.DescribirVehiculoAsync(f);
            if (string.IsNullOrEmpty(titulo) && !string.IsNullOrWhiteSpace(f.Q))
                titulo = "\"" + f.Q.Trim() + "\"";

            var (items, total) = await _catalogo.BuscarAsync(f, TamPagina);
            if (items.Count == 0 && f.Pagina > 1)
            {
                f.Pagina = 1;
                (items, total) = await _catalogo.BuscarAsync(f, TamPagina);
            }

            // RN-07: toda consulta de un usuario registrado se guarda en su historial.
            // Solo se guarda la busqueda inicial (primera pagina, sin filtros de refinamiento).
            bool esConsultaNueva = f.Pagina <= 1 && string.IsNullOrEmpty(f.Orden)
                                   && string.IsNullOrEmpty(f.Tipo) && !f.EnStock;
            if (User.Identity?.IsAuthenticated == true && esConsultaNueva)
            {
                try
                {
                    if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idUsuario))
                    {
                        var categoria = categorias.FirstOrDefault(c => c.IdCategoria == f.IdCategoria)?.Nombre;
                        await _catalogo.RegistrarConsultaAsync(idUsuario, f.IdVehiculo,
                            ArmarTexto(titulo, categoria, f.Q));
                    }
                }
                catch (Exception ex)
                {
                    // Un fallo del historial no debe impedir ver los resultados.
                    _log.LogWarning(ex, "No se pudo guardar la consulta en el historial");
                }
            }

            var vm = new ResultadosViewModel
            {
                Filtro = f,
                Titulo = titulo,
                Items = items,
                Categorias = categorias,
                Total = total,
                Pagina = f.Pagina,
                TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamPagina))
            };
            return View(vm);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al buscar repuestos");
            TempData["Error"] = "No se pudo realizar la búsqueda. Intenta de nuevo más tarde.";
            return RedirectToAction("Index", "Home");
        }
    }

    // ---------- Detalle (CU-05, CU-06, RF-07 a RF-10) ----------
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        try
        {
            var repuesto = await _catalogo.ObtenerDetalleAsync(id);
            if (repuesto is null) return NotFound();   // inexistente o desactivado (RN-05)

            var vm = new DetalleViewModel
            {
                Repuesto = repuesto,
                Vehiculos = await _catalogo.ObtenerCompatiblesAsync(id),
                Equivalencias = await _catalogo.ObtenerEquivalenciasAsync(id),
                Ofertas = await _catalogo.ObtenerOfertasAsync(id)
            };
            return View(vm);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al obtener el detalle del repuesto {Id}", id);
            TempData["Error"] = "No se pudo cargar el repuesto. Intenta de nuevo más tarde.";
            return RedirectToAction("Index", "Home");
        }
    }

    private static string ArmarTexto(string titulo, string? categoria, string? q)
    {
        var texto = titulo;
        if (!string.IsNullOrWhiteSpace(q))
            texto = string.IsNullOrEmpty(texto) ? q.Trim() : $"{texto} - {q.Trim()}";
        if (!string.IsNullOrEmpty(categoria))
            texto = string.IsNullOrEmpty(texto) ? categoria : $"{texto} ({categoria})";
        return texto.Length > 150 ? texto[..150] : texto;
    }
}
