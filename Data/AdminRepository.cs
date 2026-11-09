using Dapper;
using Repuestex.Models;
using Repuestex.ViewModels;

namespace Repuestex.Data;

public class AdminRepository
{
    private readonly Db _db;
    public AdminRepository(Db db) => _db = db;

    public async Task<List<AdminRepuesto>> ListarAsync(string? buscar)
    {
        const string sql = @"
            SELECT r.id_repuesto, r.numero_parte, r.nombre,
                   c.nombre AS categoria, r.tipo, r.activo
            FROM repuesto r
            INNER JOIN categoria c ON c.id_categoria = r.id_categoria
            WHERE (@buscar IS NULL OR r.nombre LIKE @patron OR r.numero_parte LIKE @patron)
            ORDER BY r.nombre, r.id_repuesto";
        buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();
        // Escape SQL Server LIKE wildcards so search text remains literal.
        var patron = buscar is null ? null : "%" + buscar.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        using var con = _db.Crear();
        return (await con.QueryAsync<AdminRepuesto>(sql, new { buscar, patron })).ToList();
    }

    public async Task<AdminRepuestoFormViewModel?> ObtenerAsync(int id)
    {
        const string sql = @"
            SELECT id_repuesto, numero_parte, nombre, descripcion,
                   id_categoria, tipo, imagen_url
            FROM repuesto WHERE id_repuesto = @id";
        using var con = _db.Crear();
        return await con.QuerySingleOrDefaultAsync<AdminRepuestoFormViewModel>(sql, new { id });
    }

    public async Task<bool> NumeroParteOcupadoAsync(string numeroParte, int exceptoId)
    {
        using var con = _db.Crear();
        return await con.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM repuesto
            WHERE numero_parte = @numeroParte AND id_repuesto <> @exceptoId",
            new { numeroParte, exceptoId }) > 0;
    }

    public async Task<bool> ActualizarAsync(AdminRepuestoFormViewModel m)
    {
        const string sql = @"
            UPDATE repuesto SET numero_parte = @NumeroParte,
                nombre = @Nombre, descripcion = @Descripcion,
                id_categoria = @IdCategoria, tipo = @Tipo,
                imagen_url = @ImagenUrl
            WHERE id_repuesto = @IdRepuesto";
        using var con = _db.Crear();
        return await con.ExecuteAsync(sql, m) > 0;
    }

    public async Task<bool> CambiarEstadoAsync(int id, bool activo)
    {
        using var con = _db.Crear();
        return await con.ExecuteAsync(@"
            UPDATE repuesto SET activo = @activo
            WHERE id_repuesto = @id AND activo <> @activo",
            new { id, activo }) > 0;
    }
}
