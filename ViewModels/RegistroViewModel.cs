using System.ComponentModel.DataAnnotations;

namespace Repuestex.ViewModels;

public class RegistroViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar 80 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escribe un correo valido.")]
    [StringLength(100, ErrorMessage = "El correo no puede superar 100 caracteres.")]
    [Display(Name = "Correo electronico")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena")]
    public string Contrasena { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu contrasena.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contrasenas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contrasena")]
    public string ConfirmarContrasena { get; set; } = "";
}
