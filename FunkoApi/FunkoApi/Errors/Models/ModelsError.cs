using FunkoApi.Errors.Common;

namespace FunkoApi.Errors.Models;

public static class ModelsError {
    
    // 404
    public sealed record NotFound(string Id)
        : DomainError($"No se ha encontrado el modelo con el identificador: {Id}");
    
    // 400
    public sealed record Validation(string Mensaje)
        : DomainError(Mensaje);

    // regla de negocio
    public sealed record FunkoAsociado()
        : DomainError($"No se puede eliminar el modelo puesto que tiene un Funko asociado.");
}