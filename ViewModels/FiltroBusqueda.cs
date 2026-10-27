namespace Repuestex.ViewModels;

/// <summary>Parametros de busqueda que llegan por la URL (GET).</summary>
public class FiltroBusqueda
{
    public int? IdMarca { get; set; }
    public int? IdModelo { get; set; }
    public int? Anio { get; set; }
    public int? IdVehiculo { get; set; }
    public int? IdCategoria { get; set; }
    public string? Q { get; set; }
    public string? Tipo { get; set; }
    public bool EnStock { get; set; }
    public string? Orden { get; set; }
    public int Pagina { get; set; } = 1;

    public bool TieneCriterio =>
        IdMarca is not null || IdModelo is not null || Anio is not null ||
        IdVehiculo is not null || IdCategoria is not null || !string.IsNullOrWhiteSpace(Q);
}
