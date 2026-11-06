using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Repuestex.Data;

// Permite mapear columnas id_usuario -> IdUsuario automaticamente
DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Capa de datos
builder.Services.AddSingleton<Db>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<CatalogoRepository>();
builder.Services.AddScoped<VendedorRepository>();

// Autenticacion por cookie (RF-02) con roles (RN-09)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always; // RNF-03 (HTTPS)
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US"),
    SupportedCultures = new List<System.Globalization.CultureInfo> { new("en-US") },
    SupportedUICultures = new List<System.Globalization.CultureInfo> { new("en-US") }
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Solo en desarrollo: reemplaza los hashes marcador del script SQL por hashes bcrypt reales
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var repo = scope.ServiceProvider.GetRequiredService<UsuarioRepository>();
    await repo.ReemplazarHashMarcadorAsync("admin@repuestex.com", BCrypt.Net.BCrypt.HashPassword("Admin123!"));
    await repo.ReemplazarHashMarcadorAsync("cliente@ejemplo.com", BCrypt.Net.BCrypt.HashPassword("Cliente123!"));
    await repo.ReemplazarHashMarcadorAsync("vendedor@ejemplo.com", BCrypt.Net.BCrypt.HashPassword("Vendedor123!"));

}

app.Run();
