namespace Repuestex.Models;

public class Usuario
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string ContrasenaHash { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string RolNombre { get; set; } = "";
}
