namespace Repuestex.Models;

public class AdminRepuesto
{
    public int IdRepuesto { get; set; }
    public string NumeroParte { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Tipo { get; set; } = "";
    public bool Activo { get; set; }
}
