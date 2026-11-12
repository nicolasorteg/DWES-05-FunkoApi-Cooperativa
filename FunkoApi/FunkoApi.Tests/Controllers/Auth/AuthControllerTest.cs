using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Controller.Auth;
using FunkoApi.Dto.Auth;
using FunkoApi.Errors.Auth;
using FunkoApi.Errors.Common;
using FunkoApi.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunkoApi.Tests.Controllers.Auth;

[TestFixture]
public class AuthControllerTest
{
    private Mock<IAuthService> _authServiceMock = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    private void ConfigurarUsuarioHttp(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Test]
    public async Task Login_DevuelveOkConAuthResponse_CuandoCredencialesSonValidas()
    {
        var dto = new LoginDto("aitor@funko.es", "Password123!");
        var expected = new AuthResponseDto("jwt-token-valido", "aitor@funko.es", "USER", DateTime.UtcNow.AddHours(1));

        _authServiceMock
            .Setup(s => s.Login(dto))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(expected));

        var actionResult = await _controller.Login(dto);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expected);
        _authServiceMock.Verify(s => s.Login(dto), Times.Once);
    }

    [Test]
    public async Task Login_DevuelveUnauthorized_CuandoCredencialesSonInvalidas()
    {
        var dto = new LoginDto("aitor@funko.es", "WrongPass");

        _authServiceMock
            .Setup(s => s.Login(dto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(new AuthError.Unauthorized()));

        var actionResult = await _controller.Login(dto);

        var unauthorizedResult = actionResult.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task Register_DevuelveCreatedAtAction_CuandoRegistroEsExitoso()
    {
        var dto = new SignupDto("nuevo@funko.es", "Password123!", "Aitor", "Aros");
        var expected = new AuthResponseDto("jwt-token-nuevo", "nuevo@funko.es", "USER", DateTime.UtcNow.AddHours(1));

        _authServiceMock
            .Setup(s => s.Signup(dto))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(expected));

        var actionResult = await _controller.Register(dto);

        var createdResult = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(AuthController.GetMe));
        createdResult.Value.Should().BeEquivalentTo(expected);
        _authServiceMock.Verify(s => s.Signup(dto), Times.Once);
    }

    [Test]
    public async Task Register_DevuelveConflict_CuandoEmailYaExiste()
    {
        var dto = new SignupDto("duplicado@funko.es", "Password123!", "Aitor", "Aros");

        _authServiceMock
            .Setup(s => s.Signup(dto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(new AuthError.Conflict(dto.Email)));

        var actionResult = await _controller.Register(dto);

        var conflictResult = actionResult.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Test]
    public async Task Register_DevuelveBadRequest_CuandoCreacionFalla()
    {
        var dto = new SignupDto("invalido@funko.es", "123", "Aitor", "Aros");

        _authServiceMock
            .Setup(s => s.Signup(dto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(new AuthError.CreationFailed("Contraseña débil")));

        var actionResult = await _controller.Register(dto);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task GetMe_DevuelveOkConUserDto_CuandoClaimNameIdentifierEsValido()
    {
        ConfigurarUsuarioHttp(new Claim(ClaimTypes.NameIdentifier, "42"));
        var expectedUser = new UserDto(42, "aitor@funko.es", "Aitor", "Aros", "ADMIN");

        _authServiceMock
            .Setup(s => s.GetMe(42))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(expectedUser));

        var actionResult = await _controller.GetMe();

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedUser);
        _authServiceMock.Verify(s => s.GetMe(42), Times.Once);
    }

    [Test]
    public async Task GetMe_DevuelveOkConUserDto_CuandoUsaClaimSubComoFallback()
    {
        ConfigurarUsuarioHttp(new Claim("sub", "7"));
        var expectedUser = new UserDto(7, "user@funko.es", "Diego", "Lopez", "USER");

        _authServiceMock
            .Setup(s => s.GetMe(7))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(expectedUser));

        var actionResult = await _controller.GetMe();

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedUser);
        _authServiceMock.Verify(s => s.GetMe(7), Times.Once);
    }

    [Test]
    public async Task GetMe_DevuelveUnauthorized_CuandoNoHayClaimDeIdentificador()
    {
        ConfigurarUsuarioHttp();

        var actionResult = await _controller.GetMe();

        var unauthorizedResult = actionResult.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _authServiceMock.Verify(s => s.GetMe(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task GetMe_DevuelveUnauthorized_CuandoClaimNoEsNumerico()
    {
        ConfigurarUsuarioHttp(new Claim(ClaimTypes.NameIdentifier, "no-es-un-long"));

        var actionResult = await _controller.GetMe();

        var unauthorizedResult = actionResult.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _authServiceMock.Verify(s => s.GetMe(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task GetMe_DevuelveNotFound_CuandoUsuarioNoExisteEnServicio()
    {
        ConfigurarUsuarioHttp(new Claim(ClaimTypes.NameIdentifier, "999"));

        _authServiceMock
            .Setup(s => s.GetMe(999))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(new AuthError.UserNotFound(999)));

        var actionResult = await _controller.GetMe();

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}