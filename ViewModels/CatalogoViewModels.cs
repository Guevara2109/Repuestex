using Repuestex.Models;

namespace Repuestex.ViewModels;

public class HomeViewModel
{
    public List<Marca> Marcas { get; set; } = new();
    public List<Categoria> Categorias { get; set; } = new();
    public string? Error { get; set; }
}

public class ResultadosViewModel
{
    public FiltroBusqueda Filtro { get; set; } = new();
    public string Titulo { get; set; } = "";
    public List<RepuestoResumen> Items { get; set; } = new();
    public List<Categoria> Categorias { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
}

public class DetalleViewModel
{
    public RepuestoDetalle Repuesto { get; set; } = new();
    public List<VehiculoCompatible> Vehiculos { get; set; } = new();
    public List<Equivalencia> Equivalencias { get; set; } = new();
    public List<OfertaProveedor> Ofertas { get; set; } = new();
}
