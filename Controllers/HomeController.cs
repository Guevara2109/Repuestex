using Microsoft.AspNetCore.Mvc;

namespace Repuestex.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
