using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Repuestex.Data;
using System.Text;
using System.Globalization;

namespace Repuestex.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminExtrasController : Controller
{
    private readonly Db _db;
    private readonly ILogger<AdminExtrasController> _logger;
    public AdminExtrasController(Db db, ILogger<AdminExtrasController> logger) { _db=db; _logger=logger; }

    private async Task<List<dynamic>> Rows(string sql, object? args=null)
    {
        using var db=_db.Crear();
        return (await db.QueryAsync(sql,args)).ToList();
    }
    private async Task<IActionResult> RunSave(Func<SqlConnection,Task<int>> action,string redirect)
    {
        try {
            using var db=_db.Crear();
            var count=await action(db);
            TempData[count>0?"Mensaje":"Error"]=count>0?"Cambios guardados.":"No se encontró el registro.";
        }
        catch(SqlException e) when(e.Number is 2601 or 2627 or 547 or 515 or 2628 or 245)
        { TempData["Error"]="No se pudo guardar: dato duplicado, inválido o relacionado con otros registros."; }
        catch(Exception e) { _logger.LogError(e,"Error administrativo"); TempData["Error"]="No se pudo completar la operación."; }
        return RedirectToAction(redirect);
    }

    [HttpGet]
    public async Task<IActionResult> Vehiculos()
    {
        ViewBag.Modelos=await Rows("SELECT m.id_modelo Id, CONCAT(ma.nombre,' - ',m.nombre) Nombre FROM modelo m JOIN marca ma ON ma.id_marca=m.id_marca ORDER BY ma.nombre,m.nombre");
        return View(await Rows("SELECT v.id_vehiculo Id, v.id_modelo ModeloId, CONCAT(ma.nombre,' ',m.nombre) Modelo, v.anio Anio, v.motor Motor, v.transmision Transmision, v.combustible Combustible FROM vehiculo v JOIN modelo m ON m.id_modelo=v.id_modelo JOIN marca ma ON ma.id_marca=m.id_marca ORDER BY v.id_vehiculo DESC"));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> GuardarVehiculo(int id,int modeloId,int anio,string motor,string? transmision,string? combustible)
    {
        if(modeloId<=0 || anio<2008 || anio>2026 || string.IsNullOrWhiteSpace(motor) || motor.Length>40)
        { TempData["Error"]="Revisá modelo, año (2008-2026) y motor."; return Task.FromResult<IActionResult>(RedirectToAction(nameof(Vehiculos))); }
        return RunSave(db=>db.ExecuteAsync(id==0?
            "INSERT INTO vehiculo(id_modelo,anio,motor,transmision,combustible) VALUES(@modeloId,@anio,@motor,@transmision,@combustible)":
            "UPDATE vehiculo SET id_modelo=@modeloId,anio=@anio,motor=@motor,transmision=@transmision,combustible=@combustible WHERE id_vehiculo=@id",
            new {id,modeloId,anio,motor=motor.Trim(),transmision,combustible}),nameof(Vehiculos));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> EliminarVehiculo(int id) =>
        RunSave(db=>db.ExecuteAsync("DELETE FROM vehiculo WHERE id_vehiculo=@id",new{id}),nameof(Vehiculos));

    [HttpGet]
    public async Task<IActionResult> Proveedores() => View(await Rows("SELECT id_proveedor Id,nombre Nombre,telefono Telefono,direccion Direccion,activo Activo FROM proveedor ORDER BY id_proveedor DESC"));
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> GuardarProveedor(int id,string nombre,string? telefono,string? direccion)
    {
        if(string.IsNullOrWhiteSpace(nombre)||nombre.Length>100 || telefono?.Length>20 || direccion?.Length>200)
        { TempData["Error"]="Revisá los datos del proveedor.";return Task.FromResult<IActionResult>(RedirectToAction(nameof(Proveedores))); }
        return RunSave(db=>db.ExecuteAsync(id==0?
            "INSERT INTO proveedor(nombre,telefono,direccion) VALUES(@nombre,@telefono,@direccion)":
            "UPDATE proveedor SET nombre=@nombre,telefono=@telefono,direccion=@direccion WHERE id_proveedor=@id",
            new{id,nombre=nombre.Trim(),telefono,direccion}),nameof(Proveedores));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> EstadoProveedor(int id,bool activo) =>
        RunSave(db=>db.ExecuteAsync("UPDATE proveedor SET activo=@activo WHERE id_proveedor=@id",new{id,activo}),nameof(Proveedores));

    [HttpGet]
    public async Task<IActionResult> Usuarios()
    {
        ViewBag.Roles=await Rows("SELECT id_rol Id,nombre Nombre FROM rol ORDER BY nombre");
        return View(await Rows("SELECT u.id_usuario Id,u.nombre Nombre,u.correo Correo,u.id_rol RolId,r.nombre Rol,u.activo Activo FROM usuario u JOIN rol r ON r.id_rol=u.id_rol ORDER BY u.id_usuario DESC"));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarUsuario(int id,int rolId,bool activo)
    {
        if(id<=0 || rolId<=0) return BadRequest();
        // Evitar que el administrador se quite a sí mismo los permisos.
        var claim=User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if(int.TryParse(claim,out var actual) && actual==id)
        { TempData["Error"]="No podés cambiar tu propio rol o estado.";return RedirectToAction(nameof(Usuarios)); }
        return await RunSave(db=>db.ExecuteAsync("UPDATE usuario SET id_rol=@rolId,activo=@activo WHERE id_usuario=@id AND EXISTS(SELECT 1 FROM rol WHERE id_rol=@rolId)",new{id,rolId,activo}),nameof(Usuarios));
    }

    [HttpGet]
    public IActionResult ImportarCsv()=>View();
    [HttpPost,ValidateAntiForgeryToken]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> ImportarCsv(IFormFile? archivo)
    {
        if(archivo==null || archivo.Length==0 || archivo.Length>1_000_000 || !Path.GetExtension(archivo.FileName).Equals(".csv",StringComparison.OrdinalIgnoreCase))
        { TempData["Error"]="Seleccioná un CSV de hasta 1 MB.";return RedirectToAction(nameof(ImportarCsv)); }
        // CSV simple de proveedores: nombre,telefono,direccion. Comillas y saltos dentro de campos no soportados.
        var registros=new List<(string nombre,string? telefono,string? direccion)>();
        using(var reader=new StreamReader(archivo.OpenReadStream(),Encoding.UTF8))
        {
            string? line; int number=0;
            while((line=await reader.ReadLineAsync())!=null)
            {
                number++;
                if(number==1 && line.Trim().Equals("nombre,telefono,direccion",StringComparison.OrdinalIgnoreCase))continue;
                if(string.IsNullOrWhiteSpace(line))continue;
                var fields=line.Split(',');
                if(fields.Length!=3 || string.IsNullOrWhiteSpace(fields[0]) || fields[0].Length>100 || fields[1].Length>20 || fields[2].Length>200 || number>501)
                { TempData["Error"]=$"CSV inválido en línea {number}. Máximo 500 registros; no uses comas dentro de campos.";return RedirectToAction(nameof(ImportarCsv)); }
                registros.Add((fields[0].Trim(),fields[1].Trim(),fields[2].Trim()));
            }
        }
        if(registros.Count==0){TempData["Error"]="CSV sin registros.";return RedirectToAction(nameof(ImportarCsv));}
        try
        {
            using var db=_db.Crear(); await db.OpenAsync();
            using var tx=await db.BeginTransactionAsync();
            foreach(var p in registros)
                await db.ExecuteAsync("INSERT INTO proveedor(nombre,telefono,direccion) VALUES(@nombre,@telefono,@direccion)",new{p.nombre,p.telefono,p.direccion},tx);
            await tx.CommitAsync();
            TempData["Mensaje"]=$"Se importaron {registros.Count} proveedores.";
        }
        catch(Exception e){_logger.LogError(e,"Error importando CSV");TempData["Error"]="No se importó el CSV; verificá los datos.";}
        return RedirectToAction(nameof(ImportarCsv));
    }
    [HttpGet]
    public async Task<IActionResult> Reportes()
    {
        ViewBag.Totales=await Rows(@"SELECT 'Repuestos' Nombre,COUNT(*) Total FROM repuesto UNION ALL SELECT 'Vehículos',COUNT(*) FROM vehiculo UNION ALL SELECT 'Proveedores',COUNT(*) FROM proveedor UNION ALL SELECT 'Usuarios',COUNT(*) FROM usuario UNION ALL SELECT 'Consultas',COUNT(*) FROM consulta");
        return View(await Rows("SELECT TOP 100 b.id_bitacora Id,u.nombre Usuario,b.accion Accion,b.tabla_afectada Tabla,b.fecha Fecha FROM bitacora b JOIN usuario u ON u.id_usuario=b.id_usuario ORDER BY b.fecha DESC"));
    }
}
