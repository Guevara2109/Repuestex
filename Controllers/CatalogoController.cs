using Microsoft.AspNetCore.Mvc;
using Repuestex.Data;

namespace Repuestex.Controllers;

/// <summary>Devuelve JSON para llenar los selectores en cascada marca -> modelo -> anio -> version.</summary>
public class CatalogoController : Controller
{
    private readonly CatalogoRepository _catalogo;
    private readonly ILogger<CatalogoController> _log;

    public CatalogoController(CatalogoRepository catalogo, ILogger<CatalogoController> log)
    {
        _catalogo = catalogo;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Modelos(int idMarca)
    {
        if (idMarca <= 0) return BadRequest();
        try
        {
            var datos = await _catalogo.ObtenerModelosAsync(idMarca);
            return Json(datos.Select(m => new { id = m.IdModelo, texto = m.Nombre }));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al obtener modelos");
            return StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Anios(int idModelo)
    {
        if (idModelo <= 0) return BadRequest();
        try
        {
            var datos = await _catalogo.ObtenerAniosAsync(idModelo);
            return Json(datos.Select(a => new { id = a, texto = a.ToString() }));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al obtener anios");
            return StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Versiones(int idModelo, int anio)
    {
        if (idModelo <= 0 || anio < 2008 || anio > 2026) return BadRequest();   // RN-01
        try
        {
            var datos = await _catalogo.ObtenerVersionesAsync(idModelo, anio);
            return Json(datos.Select(v => new { id = v.IdVehiculo, texto = v.Etiqueta }));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al obtener versiones");
            return StatusCode(500);
        }
    }
}
