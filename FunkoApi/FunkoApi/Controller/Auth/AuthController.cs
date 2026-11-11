using System.Security.Claims;
using FunkoApi.Dto.Auth;
using FunkoApi.Errors.Common;
using FunkoApi.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controller.Auth;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Inicia sesión en la API y obtiene el token JWT de acceso.
    /// </summary>
    /// <param name="dto">Credenciales de acceso (Email y Contraseña)</param>
    /// <returns>Token JWT con los datos de sesión y expiración</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await authService.Login(dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Registra un nuevo usuario estándar en la plataforma.
    /// </summary>
    /// <param name="dto">Datos del nuevo usuario (Email, Password, Nombre, Apellido)</param>
    /// <returns>Token JWT asignado tras el registro correcto</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] SignupDto dto)
    {
        var result = await authService.Signup(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMe), null, result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Obtiene la información del usuario autenticado actualmente. (Requiere estar autenticado)
    /// </summary>
    /// <returns>Datos del perfil del usuario extraídos a partir del token</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        // Extrae el ID del usuario directamente de los Claims del Token JWT enviado en el Header
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { mensaje = "Token inválido o sin identificador de usuario." });
        }

        var result = await authService.GetMe(userId);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }
}