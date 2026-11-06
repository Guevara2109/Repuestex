using Dapper;
using Microsoft.Data.SqlClient;
using Repuestex.Models;
using Repuestex.ViewModels;

namespace Repuestex.Data;

/// <summary>
/// Acceso a datos del panel del vendedor. Un vendedor solo ve y modifica los repuestos creados
/// por usuarios de su mismo proveedor, y solo su propia oferta (RN-12). Todo va parametrizado.
/// </summary>
public class VendedorRepository
{
    private readonly Db _db;

    public VendedorRepository(Db db) => _db = db;

    // ---------- Listado ----------
    public async Task<List<RepuestoVendedor>> ListarAsync(int idProveedor)
    {
        const string sql = @"
            SELECT r.id_repuesto, r.numero_parte, r.nombre, c.nombre AS categoria, r.tipo,
                   o.precio_ref, o.existencia
            FROM repuesto r
            JOIN categoria c ON c.id_categoria = r.id_categoria
            JOIN usuario u   ON u.id_usuario   = r.id_usuario_creador
            LEFT JOIN oferta o ON o.id_repuesto = r.id_repuesto AND o.id_proveedor = @idProveedor
            WHERE r.activo = 1 AND u.id_proveedor = @idProveedor
            ORDER BY r.nombre";
        using var con = _db.Crear();
        return (await con.QueryAsync<RepuestoVendedor>(sql, new { idProveedor })).ToList();
    }

    // ---------- Lectura para editar ----------
    public async Task<RepuestoFormViewModel?> ObtenerAsync(int idRepuesto, int idProveedor)
    {
        const string sqlRepuesto = @"
            SELECT r.id_repuesto, r.numero_parte, r.nombre, r.descripcion, r.id_categoria,
                   r.tipo, r.imagen_url, o.precio_ref, o.existencia
            FROM repuesto r
            JOIN usuario u ON u.id_usuario = r.id_usuario_creador
            LEFT JOIN oferta o ON o.id_repuesto = r.id_repuesto AND o.id_proveedor = @idProveedor
            WHERE r.id_repuesto = @idRepuesto AND r.activo = 1 AND u.id_proveedor = @idProveedor";

        using var con = _db.Crear();
        var m = await con.QuerySingleOrDefaultAsync<RepuestoFormViewModel>(sqlRepuesto, new { idRepuesto, idProveedor });
        if (m is null) return null;

        m.IdsVehiculos = (await con.QueryAsync<int>(
            "SELECT id_vehiculo FROM compatibilidad WHERE id_repuesto = @idRepuesto", new { idRepuesto })).ToList();

        var alternos = await con.QueryAsync<string>(
            "SELECT numero_alterno FROM equivalencia WHERE id_repuesto = @idRepuesto ORDER BY numero_alterno",
            new { idRepuesto });
        m.NumerosAlternos = string.Join(Environment.NewLine, alternos);
        return m;
    }

    public async Task<List<VehiculoSeleccionado>> ObtenerVehiculosAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToArray();
        if (lista.Length == 0) return new();

        const string sql = @"
            SELECT v.id_vehiculo,
                   ma.nombre + N' ' + m.nombre + N' ' + CAST(v.anio AS NVARCHAR(4)) + N' · ' + v.motor AS descripcion
            FROM vehiculo v
            JOIN modelo m ON m.id_modelo = v.id_modelo
            JOIN marca ma ON ma.id_marca = m.id_marca
            WHERE v.id_vehiculo IN @lista
            ORDER BY ma.nombre, m.nombre, v.anio";
        using var con = _db.Crear();
        return (await con.QueryAsync<VehiculoSeleccionado>(sql, new { lista })).ToList();
    }

    // RN-04: el numero de parte es unico en todo el sistema
    public async Task<bool> ExisteNumeroParteAsync(string numeroParte, int excluirIdRepuesto)
    {
        const string sql = @"SELECT COUNT(1) FROM repuesto
                             WHERE numero_parte = @numeroParte AND id_repuesto <> @excluirIdRepuesto";
        using var con = _db.Crear();
        return await con.ExecuteScalarAsync<int>(sql, new { numeroParte, excluirIdRepuesto }) > 0;
    }

    // ---------- Crear ----------
    public async Task<int> CrearAsync(RepuestoFormViewModel m, int idUsuario, int idProveedor)
    {
        const string sql = @"
            INSERT INTO repuesto (id_categoria, numero_parte, nombre, descripcion, tipo, imagen_url, id_usuario_creador)
            OUTPUT INSERTED.id_repuesto
            VALUES (@IdCategoria, @NumeroParte, @Nombre, @Descripcion, @Tipo, @ImagenUrl, @idUsuario);";

        using var con = _db.Crear();
        await con.OpenAsync();
        using var tx = con.BeginTransaction();

        int id = await con.ExecuteScalarAsync<int>(sql, new
        {
            m.IdCategoria, m.NumeroParte, m.Nombre, m.Descripcion, m.Tipo, m.ImagenUrl, idUsuario
        }, tx);

        await GuardarDetallesAsync(con, tx, id, m, idProveedor);
        tx.Commit();
        return id;
    }

    // ---------- Editar ----------
    /// <summary>Devuelve false si el repuesto no existe o no pertenece al proveedor del vendedor.</summary>
    public async Task<bool> ActualizarAsync(RepuestoFormViewModel m, int idProveedor)
    {
        const string sql = @"
            UPDATE r
               SET r.id_categoria = @IdCategoria, r.numero_parte = @NumeroParte, r.nombre = @Nombre,
                   r.descripcion = @Descripcion, r.tipo = @Tipo, r.imagen_url = @ImagenUrl
              FROM repuesto r
              JOIN usuario u ON u.id_usuario = r.id_usuario_creador
             WHERE r.id_repuesto = @IdRepuesto AND r.activo = 1 AND u.id_proveedor = @idProveedor";

        using var con = _db.Crear();
        await con.OpenAsync();
        using var tx = con.BeginTransaction();

        int filas = await con.ExecuteAsync(sql, new
        {
            m.IdCategoria, m.NumeroParte, m.Nombre, m.Descripcion, m.Tipo, m.ImagenUrl, m.IdRepuesto, idProveedor
        }, tx);
        if (filas == 0) return false;   // la transaccion se revierte al salir

        await GuardarDetallesAsync(con, tx, m.IdRepuesto, m, idProveedor);
        tx.Commit();
        return true;
    }

    // ---------- Eliminar (baja logica, RN-05 y RN-13) ----------
    public async Task<bool> EliminarAsync(int idRepuesto, int idProveedor)
    {
        const string sql = @"
            UPDATE r SET r.activo = 0
              FROM repuesto r
              JOIN usuario u ON u.id_usuario = r.id_usuario_creador
             WHERE r.id_repuesto = @idRepuesto AND r.activo = 1 AND u.id_proveedor = @idProveedor";
        using var con = _db.Crear();
        return await con.ExecuteAsync(sql, new { idRepuesto, idProveedor }) > 0;
    }

    // ---------- Compatibilidades, equivalencias y oferta (misma transaccion) ----------
    private static async Task GuardarDetallesAsync(SqlConnection con, SqlTransaction tx, int id,
                                                   RepuestoFormViewModel m, int idProveedor)
    {
        var vehiculos = m.IdsVehiculos.Distinct().ToArray();
        var alternos = m.ObtenerAlternos();

        // Compatibilidades: quita las que ya no estan y agrega las nuevas (conserva la nota de las existentes)
        await con.ExecuteAsync(
            "DELETE FROM compatibilidad WHERE id_repuesto = @id AND id_vehiculo NOT IN @vehiculos",
            new { id, vehiculos }, tx);
        await con.ExecuteAsync(@"
            INSERT INTO compatibilidad (id_repuesto, id_vehiculo)
            SELECT @id, v.id_vehiculo
            FROM vehiculo v
            WHERE v.id_vehiculo IN @vehiculos
              AND NOT EXISTS (SELECT 1 FROM compatibilidad c
                              WHERE c.id_repuesto = @id AND c.id_vehiculo = v.id_vehiculo)",
            new { id, vehiculos }, tx);

        // Equivalencias (numeros alternos)
        await con.ExecuteAsync(
            "DELETE FROM equivalencia WHERE id_repuesto = @id AND numero_alterno NOT IN @alternos",
            new { id, alternos }, tx);
        foreach (var alterno in alternos)
        {
            await con.ExecuteAsync(@"
                INSERT INTO equivalencia (id_repuesto, numero_alterno)
                SELECT @id, @alterno
                WHERE NOT EXISTS (SELECT 1 FROM equivalencia
                                  WHERE id_repuesto = @id AND numero_alterno = @alterno)",
                new { id, alterno }, tx);
        }

        // Oferta del proveedor del vendedor: actualiza o crea (RN-06: precio > 0)
        await con.ExecuteAsync(@"
            UPDATE oferta
               SET precio_ref = @PrecioRef, existencia = @Existencia, fecha_actualizacion = SYSDATETIME()
             WHERE id_proveedor = @idProveedor AND id_repuesto = @id;
            IF @@ROWCOUNT = 0
                INSERT INTO oferta (id_proveedor, id_repuesto, precio_ref, existencia, fecha_actualizacion)
                VALUES (@idProveedor, @id, @PrecioRef, @Existencia, SYSDATETIME());",
            new { idProveedor, id, m.PrecioRef, m.Existencia }, tx);
    }
}
