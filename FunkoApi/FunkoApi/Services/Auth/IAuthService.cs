using CSharpFunctionalExtensions;
using FunkoApi.Dto.Auth;
using FunkoApi.Errors.Common;

namespace FunkoApi.Services.Auth;

public interface IAuthService {
    Task<Result<AuthResponseDto, DomainError>> Login(LoginDto loginDto);
    Task<Result<AuthResponseDto, DomainError>> Signup(SignupDto signupDto);
    Task<Result<UserDto, DomainError>> GetMe(long userId);
}