using FunkoApi.Errors.Common;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Errors.Models;

public static class DomainErrorExtensions {
    
    public static IActionResult ToHttpResult(this DomainError error) => error switch { 
        
        // 404 Not Found
        ModelsError.NotFound => new NotFoundObjectResult(new { mensaje = error.Message }), 
        ModelsError.CategoriaNotFound => new NotFoundObjectResult(new { mensaje = error.Message }), 
        ModelsError.ProveedorNotFound => new NotFoundObjectResult(new { mensaje = error.Message }), 
        
        // 400 Bad Request (Errores de validación de DTOs)
        ModelsError.Validation => new BadRequestObjectResult(new { mensaje = error.Message }),
        
        // 409 Conflict (Reglas de negocio como intentar borrar con Funkos asociados)
        ModelsError.FunkoAsociado => new ConflictObjectResult(new { mensaje = error.Message }),
        
        // 500 Internal Server Error (Fallback para errores no contemplados)
        _ => new ObjectResult(new { mensaje = error.Message }) { StatusCode = 500 } };
}