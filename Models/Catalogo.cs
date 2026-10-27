namespace Repuestex.Models;

public class Marca
{
    public int IdMarca { get; set; }
    public string Nombre { get; set; } = "";
}

public class Modelo
{
    public int IdModelo { get; set; }
    public string Nombre { get; set; } = "";
}

public class Categoria
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = "";
}

/// <summary>Una version concreta de un vehiculo (motor, transmision y combustible).</summary>
public class VehiculoOpcion
{
    public int IdVehiculo { get; set; }
    public string Motor { get; set; } = "";
    public string? Transmision { get; set; }
    public string? Combustible { get; set; }

    public string Etiqueta =>
        string.Join(" · ", new[] { Motor, Transmision, Combustible }.Where(t => !string.IsNullOrWhiteSpace(t)));
}

public class RepuestoResumen
{
    public int IdRepuesto { get; set; }
    public string NumeroParte { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? ImagenUrl { get; set; }
    public decimal? PrecioDesde { get; set; }
    public int? StockTotal { get; set; }
    public int Total { get; set; }
}

public class RepuestoDetalle
{
    public int IdRepuesto { get; set; }
    public string NumeroParte { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? ImagenUrl { get; set; }
}

public class VehiculoCompatible
{
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public string Motor { get; set; } = "";
    public string? Transmision { get; set; }
    public string? Combustible { get; set; }
    public string? Nota { get; set; }
}

public class Equivalencia
{
    public string NumeroAlterno { get; set; } = "";
    public string? Fabricante { get; set; }
}

public class OfertaProveedor
{
    public string Proveedor { get; set; } = "";
    public decimal PrecioRef { get; set; }
    public int Existencia { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
