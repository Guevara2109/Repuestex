using Dapper;
using Repuestex.Models;

namespace Repuestex.Data;

/// <summary>Acceso a datos de la tabla USUARIO. Todas las consultas estan parametrizadas (evita inyeccion SQL).</summary>
public class UsuarioRepository
{
    private readonly Db _db;

    public UsuarioRepository(Db db) => _db = db;

    public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
    {
        const string sql = @"
            SELECT u.id_usuario, u.id_rol, u.nombre, u.correo, u.contrasena_hash,
                   u.activo, u.fecha_registro, r.nombre AS rol_nombre
            FROM usuario u
            JOIN rol r ON r.id_rol = u.id_rol
            WHERE u.correo = @correo";

        using var con = _db.Crear();
        return await con.QuerySingleOrDefaultAsync<Usuario>(sql, new { correo });
    }

    public async Task<bool> ExisteCorreoAsync(string correo)
    {
        const string sql = "SELECT COUNT(1) FROM usuario WHERE correo = @correo";
        using var con = _db.Crear();
        return await con.ExecuteScalarAsync<int>(sql, new { correo }) > 0;
    }

    /// <summary>Crea un usuario con rol Cliente (RF-01).</summary>
    public async Task<int> CrearClienteAsync(string nombre, string correo, string contrasenaHash)
    {
        const string sql = @"
            INSERT INTO usuario (id_rol, nombre, correo, contrasena_hash)
            OUTPUT INSERTED.id_usuario
            VALUES ((SELECT id_rol FROM rol WHERE nombre = N'Cliente'), @nombre, @correo, @contrasenaHash);";

        using var con = _db.Crear();
        return await con.ExecuteScalarAsync<int>(sql, new { nombre, correo, contrasenaHash });
    }

    /// <summary>Solo para desarrollo: cambia el hash marcador del script SQL por un hash bcrypt real.</summary>
    public async Task ReemplazarHashMarcadorAsync(string correo, string hashReal)
    {
        const string sql = @"
            UPDATE usuario SET contrasena_hash = @hashReal
            WHERE correo = @correo AND contrasena_hash = 'CAMBIAR_POR_HASH_BCRYPT'";

        using var con = _db.Crear();
        await con.ExecuteAsync(sql, new { correo, hashReal });
    }
}
