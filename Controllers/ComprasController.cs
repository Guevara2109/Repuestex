
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repuestex.Data;
using Repuestex.ViewModels;

namespace Repuestex.Controllers;

[Authorize]
public class ComprasController : Controller
{
    private readonly CatalogoRepository _catalogo;
    private readonly Db _db;
    private readonly ILogger<ComprasController> _logger;

    public ComprasController(
        CatalogoRepository catalogo,
        Db db,
        ILogger<ComprasController> logger)
    {
        _catalogo = catalogo;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Confirmar(
        int idRepuesto, int idProveedor)
    {
        if (idRepuesto <= 0 || idProveedor <= 0)
            return BadRequest();

        var repuesto = await _catalogo
            .ObtenerDetalleAsync(idRepuesto);

        if (repuesto is null)
            return NotFound();

        var ofertas = await _catalogo
            .ObtenerOfertasAsync(idRepuesto);

        var oferta = ofertas.FirstOrDefault(o =>
            o.IdProveedor == idProveedor &&
            o.Existencia > 0);

        if (oferta is null)
            return NotFound();

        return View(new CompraPreviaViewModel
        {
            IdRepuesto = idRepuesto,
            IdProveedor = idProveedor,
            NombreRepuesto = repuesto.Nombre,
            Proveedor = oferta.Proveedor,
            PrecioUnitario = oferta.PrecioRef,
            Existencia = oferta.Existencia
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Realizar(
        int idRepuesto,
        int idProveedor,
        int cantidad)
    {
        if (idRepuesto <= 0 ||
            idProveedor <= 0 ||
            cantidad <= 0)
        {
            return BadRequest("Datos de compra inválidos.");
        }

        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(identificador, out int idUsuario))
            return Challenge();

        try
        {
            using var conexion = _db.Crear();
            await conexion.OpenAsync();

            using var transaccion =
                conexion.BeginTransaction();

            try
            {
                const string actualizarExistencia = @"
                    UPDATE o
                    SET existencia = existencia - @cantidad,
                        fecha_actualizacion = SYSDATETIME()
                    OUTPUT inserted.precio_ref
                    FROM oferta o
                    INNER JOIN proveedor p
                        ON p.id_proveedor = o.id_proveedor
                    INNER JOIN repuesto r
                        ON r.id_repuesto = o.id_repuesto
                    WHERE o.id_repuesto = @idRepuesto
                      AND o.id_proveedor = @idProveedor
                      AND o.existencia >= @cantidad
                      AND p.activo = 1
                      AND r.activo = 1;";

                var precio = await conexion
                    .QuerySingleOrDefaultAsync<decimal?>(
                        actualizarExistencia,
                        new
                        {
                            idRepuesto,
                            idProveedor,
                            cantidad
                        },
                        transaccion);

                if (precio is null)
                {
                    transaccion.Rollback();

                    TempData["ErrorCompra"] =
                        "No hay existencias suficientes o el repuesto ya no está disponible.";

                    return RedirectToAction(
                        nameof(Confirmar),
                        new { idRepuesto, idProveedor });
                }

                decimal total = precio.Value * cantidad;

                const string insertarPedido = @"
                    INSERT INTO pedido
                        (id_usuario, total, estado)
                    OUTPUT INSERTED.id_pedido
                    VALUES
                        (@idUsuario, @total, N'Completada');";

                int idPedido = await conexion
                    .QuerySingleAsync<int>(
                        insertarPedido,
                        new { idUsuario, total },
                        transaccion);

                const string insertarDetalle = @"
                    INSERT INTO detalle_pedido
                        (id_pedido, id_repuesto,
                         id_proveedor, cantidad,
                         precio_unitario)
                    VALUES
                        (@idPedido, @idRepuesto,
                         @idProveedor, @cantidad,
                         @precioUnitario);";

                await conexion.ExecuteAsync(
                    insertarDetalle,
                    new
                    {
                        idPedido,
                        idRepuesto,
                        idProveedor,
                        cantidad,
                        precioUnitario = precio.Value
                    },
                    transaccion);

                transaccion.Commit();

                return RedirectToAction(
                    nameof(Exitosa),
                    new { id = idPedido });
            }
            catch
            {
                transaccion.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Error al registrar la compra");

            TempData["ErrorCompra"] =
                "No se pudo completar la compra. Intentá nuevamente.";

            return RedirectToAction(
                nameof(Confirmar),
                new { idRepuesto, idProveedor });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Exitosa(int id)
    {
        if (id <= 0)
            return NotFound();

        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(identificador, out int idUsuario))
            return Challenge();

        using var conexion = _db.Crear();

        var pedido = await conexion
            .QuerySingleOrDefaultAsync<PedidoConfirmado>(
                @"SELECT id_pedido AS IdPedido,
                         total AS Total
                  FROM pedido
                  WHERE id_pedido = @id
                    AND id_usuario = @idUsuario",
                new { id, idUsuario });

        if (pedido is null)
            return NotFound();

        return View(pedido);
    }
}

public class PedidoConfirmado
{
    public int IdPedido { get; set; }
    public decimal Total { get; set; }
}
