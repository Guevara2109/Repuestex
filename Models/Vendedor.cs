namespace Repuestex.Models;

/// <summary>Fila de la lista "Mis productos" del vendedor.</summary>
public class RepuestoVendedor
{
    public int IdRepuesto { get; set; }
    public string NumeroParte { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Tipo { get; set; } = "";
    public decimal? PrecioRef { get; set; }
    public int? Existencia { get; set; }
}

/// <summary>Vehiculo ya elegido como compatible, con su texto para mostrar.</summary>
public class VehiculoSeleccionado
{
    public int IdVehiculo { get; set; }
    public string Descripcion { get; set; } = "";
}
