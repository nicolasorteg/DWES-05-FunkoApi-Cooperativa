namespace FunkoApi.Dto.Auth;

public record UserDto(long Id, string Email, string Nombre, string Apellido, string Role);