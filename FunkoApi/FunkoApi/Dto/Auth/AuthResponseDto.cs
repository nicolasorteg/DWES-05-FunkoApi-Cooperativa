namespace FunkoApi.Dto.Auth;

public record AuthResponseDto(string Token, string Email, string Role, DateTime ExpiresAt);