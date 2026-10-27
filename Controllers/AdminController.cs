using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Repuestex.Controllers;

/// <summary>Modulo administrativo. Solo el rol Administrador puede entrar (RN-09, CA-12).</summary>
[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    public IActionResult Index() => View();
}
