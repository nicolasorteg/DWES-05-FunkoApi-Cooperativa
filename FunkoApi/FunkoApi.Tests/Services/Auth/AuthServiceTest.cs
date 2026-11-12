using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using FunkoApi.Configuration;
using FunkoApi.Dto.Auth;
using FunkoApi.Errors.Auth;
using FunkoApi.Models;
using FunkoApi.Services.Auth;
using FunkoApi.Services.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FunkoApi.Tests.Services.Auth;

[TestFixture]
public class AuthServiceTests
{
    private Mock<UserManager<AppUser>> _userManagerMock = null!;
    private Mock<SignInManager<AppUser>> _signInManagerMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private AuthService _service = null!;

    [SetUp]
    public void Setup()
    {
        _userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManagerMock = new Mock<SignInManager<AppUser>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<AppUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<AppUser>>());
        
        _emailServiceMock = new Mock<IEmailService>();

        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "ClaveSuperSecretaParaTestsDeJwt_1234567890!",
            Issuer = "FunkoApi",
            Audience = "FunkoApiClients",
            ExpirationMin = 60
        });

        _service = new AuthService(_userManagerMock.Object, _signInManagerMock.Object, _emailServiceMock.Object,  jwtOptions);
    }

    [Test]
    public async Task Login_DevuelveUnauthorized_CuandoUsuarioNoExiste()
    {
        _userManagerMock
            .Setup(m => m.FindByEmailAsync("x@x.com"))
            .ReturnsAsync((AppUser?)null);

        var result = await _service.Login(new LoginDto("x@x.com", "Pass123@"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthError.Unauthorized>();
        _signInManagerMock.Verify(
            s => s.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Test]
    public async Task Login_DevuelveUnauthorized_CuandoPasswordEsIncorrecta()
    {
        var user = new AppUser { Id = 1, Email = "a@a.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("a@a.com")).ReturnsAsync(user);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(user, "mala", false))
            .ReturnsAsync(SignInResult.Failed);

        var result = await _service.Login(new LoginDto("a@a.com", "mala"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthError.Unauthorized>();
    }

    [TestCase(new[] { "ADMIN" }, "ADMIN")]
    [TestCase(new string[0], "USER")]
    public async Task Login_DevuelveTokenConRol_CuandoCredencialesSonValidas(string[] roles, string rolEsperado)
    {
        var user = new AppUser { Id = 7, Email = "a@a.com", Nombre = "Ana", Apellidos = "Gil" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("a@a.com")).ReturnsAsync(user);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(user, "Pass123@", false))
            .ReturnsAsync(SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles.ToList());

        var result = await _service.Login(new LoginDto("a@a.com", "Pass123@"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("a@a.com");
        result.Value.Role.Should().Be(rolEsperado);
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.Token);
        jwt.Issuer.Should().Be("FunkoApi");
        jwt.Audiences.Should().Contain("FunkoApiClients");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "7");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == rolEsperado);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "Ana Gil");
    }

    [Test]
    public async Task Signup_DevuelveConflict_CuandoEmailYaExiste()
    {
        _userManagerMock
            .Setup(m => m.FindByEmailAsync("a@a.com"))
            .ReturnsAsync(new AppUser { Email = "a@a.com" });

        var result = await _service.Signup(new SignupDto("a@a.com", "Pass123@", "Ana", "Gil"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthError.Conflict>();
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Signup_DevuelveCreationFailed_CuandoIdentityFalla()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("a@a.com")).ReturnsAsync((AppUser?)null);
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), "123"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password demasiado corta" },
                new IdentityError { Description = "Falta mayuscula" }));

        var result = await _service.Signup(new SignupDto("a@a.com", "123", "Ana", "Gil"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthError.CreationFailed>();
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Signup_CreaUsuarioConRolUserYDevuelveToken_CuandoEsValido()
    {
        AppUser? creado = null;
        _userManagerMock.Setup(m => m.FindByEmailAsync("a@a.com")).ReturnsAsync((AppUser?)null);
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), "Pass123@"))
            .Callback<AppUser, string>((u, _) => creado = u)
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), "USER"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.Signup(new SignupDto("a@a.com", "Pass123@", "Ana", "Gil"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("USER");
        result.Value.Email.Should().Be("a@a.com");
        result.Value.Token.Should().NotBeNullOrWhiteSpace();
        creado.Should().NotBeNull();
        creado!.UserName.Should().Be("a@a.com");
        creado.Nombre.Should().Be("Ana");
        creado.Apellidos.Should().Be("Gil");
        creado.EmailConfirmed.Should().BeTrue();
        _userManagerMock.Verify(m => m.AddToRoleAsync(creado, "USER"), Times.Once);
    }

    [Test]
    public async Task GetMe_DevuelveUserNotFound_CuandoUsuarioNoExiste()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((AppUser?)null);

        var result = await _service.GetMe(99);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthError.UserNotFound>();
    }

    [TestCase(new[] { "ADMIN" }, "ADMIN")]
    [TestCase(new string[0], "USER")]
    public async Task GetMe_DevuelveUserDto_CuandoUsuarioExiste(string[] roles, string rolEsperado)
    {
        var user = new AppUser { Id = 5, Email = "a@a.com", Nombre = "Ana", Apellidos = "Gil" };
        _userManagerMock.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles.ToList());

        var result = await _service.GetMe(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new UserDto(5, "a@a.com", "Ana", "Gil", rolEsperado));
    }
}