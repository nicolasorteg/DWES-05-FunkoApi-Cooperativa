using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Validators.Attributes;

/// <summary>
/// Atributo personalizado que valida que un nombre no sea 'admin'.
/// </summary>
public class NoAdminAttribute : ValidationAttribute {
    
    private static readonly string[] Prohibidos = ["admin"]; // nombres prohibidos

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
        
        // si el valor no es string
        if (value is not string texto) return ValidationResult.Success;
        
        // si el texto está en la lista de prohibidos
        return Prohibidos.Contains(texto.ToLowerInvariant()) ? 
            new ValidationResult("El nombre no puede ser 'admin', 'root' o 'sistema'.") :
            ValidationResult.Success; // éxito
    }
}