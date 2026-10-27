using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Repuestex.Data;
using Repuestex.ViewModels;

namespace Repuestex.Controllers;

public class CuentaController : Controller
{
    private readonly UsuarioRepository _usuarios;
    private readonly ILogger<CuentaController> _log;

    public CuentaController(UsuarioRepository usuarios, ILogger<CuentaController> log)
    {
        _usuarios = usuarios;
        _log = log;
    }

    // ---------- REGISTRO (RF-01, CU-01, CA-01) ----------
    [HttpGet]
    public IActionResult Registro() => View(new RegistroViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var correo = modelo.Correo.Trim().ToLowerInvariant();

        // RN-10: el correo es unico
        if (await _usuarios.ExisteCorreoAsync(correo))
        {
            ModelState.AddModelError(nameof(modelo.Correo), "Este correo ya esta registrado.");
            return View(modelo);
        }

        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(modelo.Contrasena); // RNF-02
            await _usuarios.CrearClienteAsync(modelo.Nombre.Trim(), correo, hash);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601) // llave unica duplicada
        {
            // Respaldo por si dos personas se registran con el mismo correo al mismo tiempo
            ModelState.AddModelError(nameof(modelo.Correo), "Este correo ya esta registrado.");
            return View(modelo);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al registrar usuario");
            ModelState.AddModelError("", "No se pudo crear la cuenta. Intenta de nuevo mas tarde.");
            return View(modelo);
        }

        TempData["Mensaje"] = "Cuenta creada correctamente. Ya puedes iniciar sesion.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- LOGIN (RF-02, CU-02, CA-02) ----------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(modelo);

        var correo = modelo.Correo.Trim().ToLowerInvariant();
        var usuario = await _usuarios.ObtenerPorCorreoAsync(correo);

        // Mensaje unico: no se revela si fallo el correo o la contrasena
        if (usuario is null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(modelo.Contrasena, usuario.ContrasenaHash))
        {
            ModelState.AddModelError("", "Correo o contrasena incorrectos.");
            return View(modelo);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, usuario.RolNombre)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccesoDenegado() => View();
}
