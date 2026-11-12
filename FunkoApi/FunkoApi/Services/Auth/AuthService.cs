using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CSharpFunctionalExtensions;
using FunkoApi.Dto.Auth;
using FunkoApi.Errors.Auth;
using FunkoApi.Configuration;
using FunkoApi.Errors.Common;
using FunkoApi.Models;
using FunkoApi.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FunkoApi.Services.Auth;

public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IEmailService emailService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<Result<AuthResponseDto, DomainError>> Login(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user is null)
            return Result.Failure<AuthResponseDto, DomainError>(new AuthError.Unauthorized());

        var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
            return Result.Failure<AuthResponseDto, DomainError>(new AuthError.Unauthorized());
        
        string subject = "Nuevo inicio de sesión detectado";
        string htmlBody = $"<h3>Hola {user.UserName},</h3><p>Se ha detectado un nuevo inicio de sesión en tu cuenta de FunkoApi Store.</p><hr><small>Si no has sido tú, contacta con soporte.</small>";
        
        var emailResult = await emailService.SendEmailAsync(user.Email!, subject, htmlBody);
        
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "USER";

        var tokenResponse = GenerateJwtToken(user, role);
        return Result.Success<AuthResponseDto, DomainError>(tokenResponse);
    }

    public async Task<Result<AuthResponseDto, DomainError>> Signup(SignupDto signupDto)
    {
        var existingUser = await userManager.FindByEmailAsync(signupDto.Email);
        if (existingUser is not null)
            return Result.Failure<AuthResponseDto, DomainError>(new AuthError.Conflict(signupDto.Email));

        var user = new AppUser
        {
            UserName = signupDto.Email,
            Email = signupDto.Email,
            Nombre = signupDto.Nombre,
            Apellidos = signupDto.Apellido,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, signupDto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<AuthResponseDto, DomainError>(new AuthError.CreationFailed(errors));
        }

        await userManager.AddToRoleAsync(user, "USER");
        const string role = "USER";

        var tokenResponse = GenerateJwtToken(user, role);
        return Result.Success<AuthResponseDto, DomainError>(tokenResponse);
    }

    public async Task<Result<UserDto, DomainError>> GetMe(long userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<UserDto, DomainError>(new AuthError.UserNotFound(userId));

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "USER";

        var userDto = new UserDto(user.Id, user.Email ?? "", user.Nombre, user.Apellidos, role);
        return Result.Success<UserDto, DomainError>(userDto);
    }

    private AuthResponseDto GenerateJwtToken(AppUser user, string role)
    {
        var settings = jwtOptions.Value;
        var expires = DateTime.UtcNow.AddMinutes(settings.ExpirationMin);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, $"{user.Nombre} {user.Apellidos}".Trim())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDto(tokenString, user.Email ?? "", role, expires);
    }
}