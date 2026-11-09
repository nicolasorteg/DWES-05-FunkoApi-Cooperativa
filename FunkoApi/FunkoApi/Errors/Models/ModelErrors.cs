using FunkoApi.Errors.Common;

namespace FunkoApi.Errors.Models;

/// <summary>
/// Factory para crear errores de los modelos
/// </summary>
public class ModelErrors {
    
    public static DomainError NotFound(int id) =>
        new ModelsError.NotFound(id.ToString());

    public static DomainError Validation(string mensaje) =>
        new ModelsError.Validation(mensaje);

    public static DomainError FunkoAsociado(int id) =>
        new ModelsError.FunkoAsociado(id);
}