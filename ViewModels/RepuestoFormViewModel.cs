using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Repuestex.Models;

namespace Repuestex.ViewModels;

/// <summary>Formulario para crear o editar un producto (repuesto + oferta del vendedor).</summary>
public class RepuestoFormViewModel
{
    public int IdRepuesto { get; set; }

    [Required(ErrorMessage = "El número de parte es obligatorio.")]
    [StringLength(40, ErrorMessage = "El número de parte no puede superar 40 caracteres.")]
    [Display(Name = "Número de parte")]
    public string NumeroParte { get; set; } = "";

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar 120 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    [StringLength(2000, ErrorMessage = "La descripción no puede superar 2000 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public int? IdCategoria { get; set; }

    [Required(ErrorMessage = "Elige el tipo.")]
    [Display(Name = "Tipo")]
    public string Tipo { get; set; } = "Original";

    [StringLength(255, ErrorMessage = "La dirección de la imagen no puede superar 255 caracteres.")]
    [Display(Name = "Imagen (URL)")]
    public string? ImagenUrl { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 99999999.99, ErrorMessage = "El precio debe ser mayor que cero.")]   // RN-06
    [Display(Name = "Precio referencial")]
    public decimal? PrecioRef { get; set; }

    [Required(ErrorMessage = "La existencia es obligatoria.")]
    [Range(0, 1000000, ErrorMessage = "La existencia no puede ser negativa.")]
    [Display(Name = "Existencia")]
    public int? Existencia { get; set; }

    /// <summary>Vehiculos compatibles (RN-03: al menos uno).</summary>
    public List<int> IdsVehiculos { get; set; } = new();

    [StringLength(2000, ErrorMessage = "La lista de números alternos es demasiado larga.")]
    [Display(Name = "Números de parte alternos")]
    public string? NumerosAlternos { get; set; }

    // ---- Solo para dibujar la vista (no se reciben del formulario) ----
    [BindNever, ValidateNever] public List<Categoria> Categorias { get; set; } = new();
    [BindNever, ValidateNever] public List<Marca> Marcas { get; set; } = new();
    [BindNever, ValidateNever] public List<VehiculoSeleccionado> VehiculosSeleccionados { get; set; } = new();

    /// <summary>Numeros alternos separados por salto de linea o coma, sin vacios ni repetidos.</summary>
    public List<string> ObtenerAlternos() =>
        (NumerosAlternos ?? "")
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .Where(a => a.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
