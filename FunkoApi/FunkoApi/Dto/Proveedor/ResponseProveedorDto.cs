using System.Text.Json.Serialization;
using FunkoApi.Validators.Attributes;

namespace FunkoApi.Dto.Proveedor;

public record ResponseProveedorDto( 
    [property: JsonPropertyName("id")] string Id, // objectid -> string
    [property: JsonPropertyName("nombre")] [NoAdmin] string Nombre, // no admin
    [property: JsonPropertyName("email")] string Email, 
    [property: JsonPropertyName("telefono")] string? Telefono 
);