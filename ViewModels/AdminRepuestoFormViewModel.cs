using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Repuestex.Models;

namespace Repuestex.ViewModels;

public class AdminRepuestoFormViewModel
{
    public int IdRepuesto { get; set; }
    [Required, StringLength(40)]
    [Display(Name = "Número de parte")]
    public string NumeroParte { get; set; } = "";
    [Required, StringLength(120)]
    public string Nombre { get; set; } = "";
    [StringLength(2000)]
    public string? Descripcion { get; set; }
    [Required(ErrorMessage = "Selecciona una categoría")]
    [Display(Name = "Categoría")]
    public int? IdCategoria { get; set; }
    [Required]
    public string Tipo { get; set; } = "Original";
    [StringLength(255)]
    [Display(Name = "Imagen (URL)")]
    public string? ImagenUrl { get; set; }
    [ValidateNever]
    public List<Categoria> Categorias { get; set; } = new();
}
