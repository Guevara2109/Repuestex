using System.ComponentModel.DataAnnotations;

namespace Repuestex.ViewModels;

public class CompraPreviaViewModel
{
    public int IdRepuesto { get; set; }
    public int IdProveedor { get; set; }
    public string NombreRepuesto { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public int Existencia { get; set; }
    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; } = 1;
}
