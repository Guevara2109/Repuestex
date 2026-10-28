using Dapper;
using Repuestex.Models;
using Repuestex.ViewModels;

namespace Repuestex.Data;

/// <summary>Consultas del catalogo: vehiculos, repuestos, compatibilidades, equivalencias y ofertas.</summary>
public class CatalogoRepository
{
    private readonly Db _db;

    public CatalogoRepository(Db db) => _db = db;

    // ---------- Selectores en cascada (CU-03) ----------
    public async Task<List<Marca>> ObtenerMarcasAsync()
    {
        using var con = _db.Crear();
        var r = await con.QueryAsync<Marca>("SELECT id_marca, nombre FROM marca ORDER BY nombre");
        return r.ToList();
    }

    public async Task<List<Modelo>> ObtenerModelosAsync(int idMarca)
    {
        const string sql = @"
            SELECT m.id_modelo, m.nombre
            FROM modelo m
            WHERE m.id_marca = @idMarca
              AND EXISTS (SELECT 1 FROM vehiculo v WHERE v.id_modelo = m.id_modelo)
            ORDER BY m.nombre";
        using var con = _db.Crear();
        var r = await con.QueryAsync<Modelo>(sql, new { idMarca });
        return r.ToList();
    }

    public async Task<List<int>> ObtenerAniosAsync(int idModelo)
    {
        const string sql = @"
            SELECT DISTINCT CAST(anio AS INT) AS valor
            FROM vehiculo
            WHERE id_modelo = @idModelo
            ORDER BY valor DESC";
        using var con = _db.Crear();
        var r = await con.QueryAsync<int>(sql, new { idModelo });
        return r.ToList();
    }

    public async Task<List<VehiculoOpcion>> ObtenerVersionesAsync(int idModelo, int anio)
    {
        const string sql = @"
            SELECT id_vehiculo, motor, transmision, combustible
            FROM vehiculo
            WHERE id_modelo = @idModelo AND anio = @anio
            ORDER BY motor, transmision, combustible";
        using var con = _db.Crear();
        var r = await con.QueryAsync<VehiculoOpcion>(sql, new { idModelo, anio });
        return r.ToList();
    }

    public async Task<List<Categoria>> ObtenerCategoriasAsync()
    {
        using var con = _db.Crear();
        var r = await con.QueryAsync<Categoria>("SELECT id_categoria, nombre FROM categoria ORDER BY nombre");
        return r.ToList();
    }

    /// <summary>Texto del vehiculo buscado, por ejemplo "Toyota Corolla 2015 1.8L 4 cilindros".</summary>
    public async Task<string> DescribirVehiculoAsync(FiltroBusqueda f)
    {
        using var con = _db.Crear();

        if (f.IdVehiculo is int idVehiculo)
        {
            var d = await con.QuerySingleOrDefaultAsync<string>(@"
                SELECT ma.nombre + N' ' + m.nombre + N' ' + CAST(v.anio AS NVARCHAR(4)) + N' ' + v.motor
                FROM vehiculo v
                JOIN modelo m ON m.id_modelo = v.id_modelo
                JOIN marca ma ON ma.id_marca = m.id_marca
                WHERE v.id_vehiculo = @idVehiculo", new { idVehiculo });
            if (d is not null) return d;
        }

        if (f.IdModelo is int idModelo)
        {
            var d = await con.QuerySingleOrDefaultAsync<string>(@"
                SELECT ma.nombre + N' ' + m.nombre
                FROM modelo m
                JOIN marca ma ON ma.id_marca = m.id_marca
                WHERE m.id_modelo = @idModelo", new { idModelo });
            if (d is not null) return f.Anio is int a ? $"{d} {a}" : d;
        }

        if (f.IdMarca is int idMarca)
        {
            var d = await con.QuerySingleOrDefaultAsync<string>(
                "SELECT nombre FROM marca WHERE id_marca = @idMarca", new { idMarca });
            if (d is not null) return d;
        }

        return "";
    }

    // ---------- Busqueda (CU-03, CU-04) ----------
    private static string EscaparLike(string texto) =>
        texto.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    public async Task<(List<RepuestoResumen> Items, int Total)> BuscarAsync(FiltroBusqueda f, int tamPagina)
    {
        // El orden viene de una lista cerrada: nunca se concatena texto del usuario al SQL.
        string orden = f.Orden switch
        {
            "precio_asc"  => "CASE WHEN precio_desde IS NULL THEN 1 ELSE 0 END, precio_desde ASC, nombre ASC",
            "precio_desc" => "CASE WHEN precio_desde IS NULL THEN 1 ELSE 0 END, precio_desde DESC, nombre ASC",
            "nombre_desc" => "nombre DESC",
            _             => "nombre ASC"
        };

        string sql = $@"
            WITH base AS (
                SELECT r.id_repuesto, r.numero_parte, r.nombre, c.nombre AS categoria, r.tipo, r.imagen_url,
                       (SELECT MIN(o.precio_ref)
                          FROM oferta o
                          JOIN proveedor p ON p.id_proveedor = o.id_proveedor
                         WHERE o.id_repuesto = r.id_repuesto AND p.activo = 1) AS precio_desde,
                       (SELECT SUM(o.existencia)
                          FROM oferta o
                          JOIN proveedor p ON p.id_proveedor = o.id_proveedor
                         WHERE o.id_repuesto = r.id_repuesto AND p.activo = 1) AS stock_total
                FROM repuesto r
                JOIN categoria c ON c.id_categoria = r.id_categoria
                WHERE r.activo = 1
                  AND (@idCategoria IS NULL OR r.id_categoria = @idCategoria)
                  AND (@tipo IS NULL OR r.tipo = @tipo)
                  AND (@enStock = 0 OR EXISTS (
                        SELECT 1 FROM oferta o
                        JOIN proveedor p ON p.id_proveedor = o.id_proveedor
                        WHERE o.id_repuesto = r.id_repuesto AND p.activo = 1 AND o.existencia > 0))
                  AND (@q IS NULL
                       OR r.nombre LIKE @qLike
                       OR r.numero_parte LIKE @qLike
                       OR EXISTS (SELECT 1 FROM equivalencia e
                                  WHERE e.id_repuesto = r.id_repuesto AND e.numero_alterno LIKE @qLike))
                  AND (
                        (@idMarca IS NULL AND @idModelo IS NULL AND @anio IS NULL AND @idVehiculo IS NULL)
                        OR EXISTS (
                            SELECT 1
                            FROM compatibilidad cp
                            JOIN vehiculo v ON v.id_vehiculo = cp.id_vehiculo
                            JOIN modelo m   ON m.id_modelo   = v.id_modelo
                            WHERE cp.id_repuesto = r.id_repuesto
                              AND (@idMarca IS NULL OR m.id_marca = @idMarca)
                              AND (@idModelo IS NULL OR v.id_modelo = @idModelo)
                              AND (@anio IS NULL OR v.anio = @anio)
                              AND (@idVehiculo IS NULL OR v.id_vehiculo = @idVehiculo)
                        )
                      )
            )
            SELECT id_repuesto, numero_parte, nombre, categoria, tipo, imagen_url,
                   precio_desde, stock_total, COUNT(*) OVER() AS total
            FROM base
            ORDER BY {orden}
            OFFSET @desde ROWS FETCH NEXT @tam ROWS ONLY;";

        string? q = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();
        string? qLike = q is null ? null : "%" + EscaparLike(q) + "%";
        string? tipo = string.IsNullOrWhiteSpace(f.Tipo) ? null : f.Tipo;
        int pagina = f.Pagina < 1 ? 1 : f.Pagina;

        var parametros = new
        {
            idMarca = f.IdMarca,
            idModelo = f.IdModelo,
            anio = f.Anio,
            idVehiculo = f.IdVehiculo,
            idCategoria = f.IdCategoria,
            q,
            qLike,
            tipo,
            enStock = f.EnStock,
            desde = (pagina - 1) * tamPagina,
            tam = tamPagina
        };

        using var con = _db.Crear();
        var items = (await con.QueryAsync<RepuestoResumen>(sql, parametros)).ToList();
        return (items, items.FirstOrDefault()?.Total ?? 0);
    }

    // ---------- Sugerencias mientras se escribe ----------
    public async Task<List<RepuestoResumen>> ObtenerSugerenciasAsync(string texto)
    {
        const string sql = @"
            SELECT TOP (6) r.id_repuesto, r.numero_parte, r.nombre, c.nombre AS categoria, r.tipo
            FROM repuesto r
            JOIN categoria c ON c.id_categoria = r.id_categoria
            WHERE r.activo = 1
              AND (r.nombre LIKE @qLike
                   OR r.numero_parte LIKE @qLike
                   OR EXISTS (SELECT 1 FROM equivalencia e
                              WHERE e.id_repuesto = r.id_repuesto AND e.numero_alterno LIKE @qLike))
            ORDER BY CASE WHEN r.nombre LIKE @qInicio OR r.numero_parte LIKE @qInicio THEN 0 ELSE 1 END, r.nombre";

        string escapado = EscaparLike(texto);
        string qLike = "%" + escapado + "%";
        string qInicio = escapado + "%";

        using var con = _db.Crear();
        return (await con.QueryAsync<RepuestoResumen>(sql, new { qLike, qInicio })).ToList();
    }

    // ---------- Detalle (CU-05, CU-06) ----------
    public async Task<RepuestoDetalle?> ObtenerDetalleAsync(int id)
    {
        const string sql = @"
            SELECT r.id_repuesto, r.numero_parte, r.nombre, r.descripcion,
                   c.nombre AS categoria, r.tipo, r.imagen_url
            FROM repuesto r
            JOIN categoria c ON c.id_categoria = r.id_categoria
            WHERE r.id_repuesto = @id AND r.activo = 1";   // RN-05
        using var con = _db.Crear();
        return await con.QuerySingleOrDefaultAsync<RepuestoDetalle>(sql, new { id });
    }

    public async Task<List<VehiculoCompatible>> ObtenerCompatiblesAsync(int id)
    {
        const string sql = @"
            SELECT ma.nombre AS marca, m.nombre AS modelo, CAST(v.anio AS INT) AS anio,
                   v.motor, v.transmision, v.combustible, cp.nota
            FROM compatibilidad cp
            JOIN vehiculo v ON v.id_vehiculo = cp.id_vehiculo
            JOIN modelo m   ON m.id_modelo   = v.id_modelo
            JOIN marca ma   ON ma.id_marca   = m.id_marca
            WHERE cp.id_repuesto = @id
            ORDER BY ma.nombre, m.nombre, v.anio";
        using var con = _db.Crear();
        return (await con.QueryAsync<VehiculoCompatible>(sql, new { id })).ToList();
    }

    public async Task<List<Equivalencia>> ObtenerEquivalenciasAsync(int id)
    {
        const string sql = @"
            SELECT numero_alterno, fabricante
            FROM equivalencia
            WHERE id_repuesto = @id
            ORDER BY numero_alterno";
        using var con = _db.Crear();
        return (await con.QueryAsync<Equivalencia>(sql, new { id })).ToList();
    }

    public async Task<List<OfertaProveedor>> ObtenerOfertasAsync(int id)
    {
        const string sql = @"
            SELECT p.nombre AS proveedor, o.precio_ref, o.existencia, o.fecha_actualizacion
            FROM oferta o
            JOIN proveedor p ON p.id_proveedor = o.id_proveedor
            WHERE o.id_repuesto = @id AND p.activo = 1
            ORDER BY o.precio_ref";
        using var con = _db.Crear();
        return (await con.QueryAsync<OfertaProveedor>(sql, new { id })).ToList();
    }

    // ---------- Historial (RN-07, CU-08) ----------
    public async Task RegistrarConsultaAsync(int idUsuario, int? idVehiculo, string? texto)
    {
        const string sql = @"
            INSERT INTO consulta (id_usuario, id_vehiculo, texto_busqueda)
            VALUES (@idUsuario, @idVehiculo, @texto)";
        using var con = _db.Crear();
        await con.ExecuteAsync(sql, new { idUsuario, idVehiculo, texto });
    }
}
