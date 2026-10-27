using System.ComponentModel.DataAnnotations;

namespace Repuestex.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escribe un correo valido.")]
    [Display(Name = "Correo electronico")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena")]
    public string Contrasena { get; set; } = "";
}
