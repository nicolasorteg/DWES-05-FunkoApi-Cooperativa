using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FunkoApi.Validators.Attributes;

namespace FunkoApi.Dto.Proveedor;

public record UpdateProveedorDto( 
    [property: JsonPropertyName("nombre")] 
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")] 
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")] 
    [NoAdmin] 
    string? Nombre, 
    
    [property: JsonPropertyName("email")] 
    [Required(ErrorMessage = "El email es obligatorio.")] 
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    string? Email, 
    
    [property: JsonPropertyName("telefono")] 
    string? Telefono 
);