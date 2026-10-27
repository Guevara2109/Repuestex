using Microsoft.AspNetCore.Mvc;
using Repuestex.Data;
using Repuestex.ViewModels;

namespace Repuestex.Controllers;

public class HomeController : Controller
{
    private readonly CatalogoRepository _catalogo;
    private readonly ILogger<HomeController> _log;

    public HomeController(CatalogoRepository catalogo, ILogger<HomeController> log)
    {
        _catalogo = catalogo;
        _log = log;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel();
        try
        {
            vm.Marcas = await _catalogo.ObtenerMarcasAsync();
            vm.Categorias = await _catalogo.ObtenerCategoriasAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "No se pudo cargar el catalogo en la pagina de inicio");
            vm.Error = "No se pudo cargar el catálogo. Intenta de nuevo más tarde.";
        }
        return View(vm);
    }

    public IActionResult Error() => View();
}
