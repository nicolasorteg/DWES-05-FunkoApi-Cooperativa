using System.Text.Json.Serialization;

namespace FunkoApi.Dto.Categoria;

public record ResponseCategoriaDto( 
    [property: JsonPropertyName("id")] string Id, // id estilo yt
    [property: JsonPropertyName("nombre")] string Nombre 
);