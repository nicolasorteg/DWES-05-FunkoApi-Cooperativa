using FunkoApi.Errors.Common;

namespace FunkoApi.Errors.Models;

/// <summary>
/// Factory para crear errores de los modelos
/// </summary>
public class ModelErrors {
    
    public static DomainError NotFound(string id) =>
        new ModelsError.NotFound(id);

    public static DomainError Validation(string mensaje) =>
        new ModelsError.Validation(mensaje);

    public static DomainError FunkoAsociado() =>
        new ModelsError.FunkoAsociado();
}