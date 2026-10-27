using Microsoft.Data.SqlClient;

namespace Repuestex.Data;

/// <summary>Fabrica de conexiones a SQL Server. Una sola cadena de conexion para todo el sistema.</summary>
public class Db
{
    private readonly string _cadena;

    public Db(IConfiguration configuracion)
    {
        _cadena = configuracion.GetConnectionString("Repuestex")
            ?? throw new InvalidOperationException("Falta la cadena de conexion 'Repuestex' en appsettings.json.");
    }

    public SqlConnection Crear() => new SqlConnection(_cadena);
}
