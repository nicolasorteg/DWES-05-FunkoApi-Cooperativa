using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Bson;
using Moq;
using FunkoApi.Dto.Proveedor;
using FunkoApi.Errors.Models;
using FunkoApi.Models;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Repositories.Proveedores;
using FunkoApi.Services.Cache;
using FunkoApi.Services.Proveedor;
using FunkoApi.Services.Proveedores;

namespace FunkoApi.Tests.Services.Proveedores;

[TestFixture]
public class ProveedorServiceTests
{
    // object ids validos
    private const string IdValido = "507f1f77bcf86cd799439011";
    private const string IdInexistente = "507f1f77bcf86cd799439099";

    private Mock<IProveedorRepository> _proveedorRepoMock = null!;
    private Mock<IFunkosRepository> _funkoRepoMock = null!;
    private Mock<IValidator<CreateProveedorDto>> _createValidatorMock = null!;
    private Mock<IValidator<UpdateProveedorDto>> _updateValidatorMock = null!;
    private Mock<ICacheService> _cacheMock = null!;

    private ProveedorService _service = null!;

    // helpers
    private static Proveedor NuevoProveedor(string id = IdValido, string nombre = "Bandai") =>
        new() { Id = ObjectId.Parse(id), Nombre = nombre };

    private static ResponseProveedorDto NuevoResponse(string id = IdValido, string nombre = "Bandai") =>
        new(id, nombre, "prueba@prueba.com", "666666666");

    private static CreateProveedorDto NuevoCreateDto(string nombre = "Bandai") => new(nombre, "prueba@prueba.com", "666666666");
    private static UpdateProveedorDto NuevoUpdateDto(string nombre = "Bandai Editado") => new(nombre, "prueba@prueba.com", "666666666");

    [SetUp]
    public void Setup()
    {
        _proveedorRepoMock = new Mock<IProveedorRepository>();
        _funkoRepoMock = new Mock<IFunkosRepository>();
        _createValidatorMock = new Mock<IValidator<CreateProveedorDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateProveedorDto>>();
        _cacheMock = new Mock<ICacheService>();

        _service = new ProveedorService(
            _proveedorRepoMock.Object,
            _funkoRepoMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _cacheMock.Object
        );
    }

    /// captura las keys
    private List<string> CapturarClavesPaginadas()
    {
        var keys = new List<string>();
        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ResponseProveedorDto>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((k, _) => keys.Add(k))
            .ReturnsAsync((IReadOnlyList<ResponseProveedorDto>?)null);
        _proveedorRepoMock
            .Setup(r => r.GetAllPagedAsync(1, 10))
            .ReturnsAsync(new List<Proveedor>());
        return keys;
    }

    private void SetupValidacionOk(bool create)
    {
        if (create)
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CreateProveedorDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        else
            _updateValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateProveedorDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
    }


    [Test]
    public async Task GetAllPagedAsync_DevuelveDesdeCache_CuandoExisteEnCache()
    {
        var cachedList = new List<ResponseProveedorDto> { NuevoResponse() };
        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ResponseProveedorDto>>(
                It.Is<string>(k => k.StartsWith("proveedores:paged:")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedList);

        var result = await _service.GetAllPagedAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Nombre.Should().Be("Bandai");
        _proveedorRepoMock.Verify(r => r.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetAllPagedAsync_ConsultaRepoYGuardaEnCache_CuandoNoExisteEnCache()
    {
        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ResponseProveedorDto>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ResponseProveedorDto>?)null);
        _proveedorRepoMock
            .Setup(r => r.GetAllPagedAsync(1, 10))
            .ReturnsAsync(new List<Proveedor> { NuevoProveedor() });

        var result = await _service.GetAllPagedAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _cacheMock.Verify(
            c => c.SetAsync(
                It.Is<string>(k => k.StartsWith("proveedores:paged:") && k.EndsWith(":1:10")),
                It.IsAny<List<ResponseProveedorDto>>(),
                It.IsAny<TimeSpan>()),
            Times.Once);
    }


    [Test]
    public async Task GetByIdAsync_DevuelveDesdeCache_CuandoExisteEnCache()
    {
        _cacheMock
            .Setup(c => c.GetAsync<ResponseProveedorDto>($"proveedor:{IdValido}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NuevoResponse());

        var result = await _service.GetByIdAsync(IdValido);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Bandai");
        _proveedorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_DevuelveNotFound_CuandoNoExisteEnRepo()
    {
        _cacheMock
            .Setup(c => c.GetAsync<ResponseProveedorDto>($"proveedor:{IdInexistente}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResponseProveedorDto?)null);
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdInexistente))
            .ReturnsAsync((Proveedor?)null);

        var result = await _service.GetByIdAsync(IdInexistente);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound(IdInexistente));
    }

    [Test]
    public async Task GetByIdAsync_DevuelveDtoYGuardaEnCache_CuandoExisteEnRepo()
    {
        _cacheMock
            .Setup(c => c.GetAsync<ResponseProveedorDto>($"proveedor:{IdValido}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResponseProveedorDto?)null);
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdValido))
            .ReturnsAsync(NuevoProveedor());

        var result = await _service.GetByIdAsync(IdValido);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(IdValido);
        _cacheMock.Verify(
            c => c.SetAsync($"proveedor:{IdValido}", It.IsAny<ResponseProveedorDto>(), It.IsAny<TimeSpan>()),
            Times.Once);
    }
    
    [Test]
    public async Task AddAsync_DevuelveErrorValidacion_CuandoDtoEsInvalido()
    {
        var dto = NuevoCreateDto("");
        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Nombre", "El nombre es obligatorio") }));

        var result = await _service.AddAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.Validation("El nombre es obligatorio"));
        _proveedorRepoMock.Verify(r => r.AddAsync(It.IsAny<Proveedor>()), Times.Never);
    }

    [Test]
    public async Task AddAsync_UneMensajesDeValidacion_CuandoHayVariosErrores()
    {
        var dto = NuevoCreateDto("");
        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Nombre", "Error A"),
                new ValidationFailure("Nombre", "Error B")
            }));

        var result = await _service.AddAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.Validation("Error A, Error B"));
    }

    [Test]
    public async Task AddAsync_CreaProveedorEInvalidaCachePaginada_CuandoDtoEsValido()
    {
        var keys = CapturarClavesPaginadas();
        var dto = NuevoCreateDto("Funko Inc");
        SetupValidacionOk(create: true);
        _proveedorRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Proveedor>()))
            .ReturnsAsync(NuevoProveedor(IdValido, "Funko Inc"));

        await _service.GetAllPagedAsync();          // key antes
        var result = await _service.AddAsync(dto);
        await _service.GetAllPagedAsync();          // key después

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Funko Inc");
        _proveedorRepoMock.Verify(r => r.AddAsync(It.IsAny<Proveedor>()), Times.Once);
        keys.Should().HaveCount(2);
        keys[0].Should().NotBe(keys[1]);
    }
    
    [Test]
    public async Task UpdateAsync_DevuelveErrorValidacion_CuandoDtoEsInvalido()
    {
        var dto = NuevoUpdateDto("");
        _updateValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Nombre", "El nombre es obligatorio") }));

        var result = await _service.UpdateAsync(IdValido, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.Validation("El nombre es obligatorio"));
        _proveedorRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Proveedor>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_DevuelveNotFound_CuandoProveedorNoExiste()
    {
        var dto = NuevoUpdateDto();
        SetupValidacionOk(create: false);
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdInexistente))
            .ReturnsAsync((Proveedor?)null);

        var result = await _service.UpdateAsync(IdInexistente, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound(IdInexistente));
        _proveedorRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Proveedor>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_DevuelveNotFound_CuandoElRepoDevuelveNullAlActualizar()
    {
        var dto = NuevoUpdateDto();
        SetupValidacionOk(create: false);
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdValido))
            .ReturnsAsync(NuevoProveedor());
        _proveedorRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Proveedor>()))
            .ReturnsAsync((Proveedor?)null);

        var result = await _service.UpdateAsync(IdValido, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound(IdValido));
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_ActualizaProveedorEInvalidaCache_CuandoEsValido()
    {
        var keys = CapturarClavesPaginadas();
        var dto = NuevoUpdateDto("Editado");
        var existente = NuevoProveedor();
        SetupValidacionOk(create: false);
        _proveedorRepoMock.Setup(r => r.GetByIdAsync(IdValido)).ReturnsAsync(existente);
        _proveedorRepoMock
            .Setup(r => r.UpdateAsync(existente))   // el servicio muta y reenvía la entidad existente
            .ReturnsAsync(NuevoProveedor(IdValido, "Bandai Editado"));

        await _service.GetAllPagedAsync();
        var result = await _service.UpdateAsync(IdValido, dto);
        await _service.GetAllPagedAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Bandai Editado");
        _proveedorRepoMock.Verify(r => r.UpdateAsync(existente), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"proveedor:{IdValido}"), Times.Once);
        keys[0].Should().NotBe(keys[1]);
    }
    

    [Test]
    public async Task DeleteAsync_DevuelveNotFound_CuandoProveedorNoExiste()
    {
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdInexistente))
            .ReturnsAsync((Proveedor?)null);

        var result = await _service.DeleteAsync(IdInexistente);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound(IdInexistente));
        _funkoRepoMock.Verify(r => r.ExistsByProveedorAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_DevuelveErrorFunkoAsociado_CuandoTieneFunkosAsignados()
    {
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdValido))
            .ReturnsAsync(NuevoProveedor());
        _funkoRepoMock
            .Setup(r => r.ExistsByProveedorAsync(IdValido))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync(IdValido);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.FunkoAsociado());
        _proveedorRepoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_DevuelveNotFound_CuandoElRepoNoPuedeBorrar()
    {
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdValido))
            .ReturnsAsync(NuevoProveedor());
        _funkoRepoMock
            .Setup(r => r.ExistsByProveedorAsync(IdValido))
            .ReturnsAsync(false);
        _proveedorRepoMock
            .Setup(r => r.DeleteAsync(IdValido))
            .ReturnsAsync(false);

        var result = await _service.DeleteAsync(IdValido);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound(IdValido));
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_EliminaEInvalidaCache_CuandoEsValidoYNoTieneFunkos()
    {
        var keys = CapturarClavesPaginadas();
        _proveedorRepoMock
            .Setup(r => r.GetByIdAsync(IdValido))
            .ReturnsAsync(NuevoProveedor());
        _funkoRepoMock
            .Setup(r => r.ExistsByProveedorAsync(IdValido))
            .ReturnsAsync(false);
        _proveedorRepoMock
            .Setup(r => r.DeleteAsync(IdValido))
            .ReturnsAsync(true);

        await _service.GetAllPagedAsync();
        var result = await _service.DeleteAsync(IdValido);
        await _service.GetAllPagedAsync();

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(c => c.RemoveAsync($"proveedor:{IdValido}"), Times.Once);
        keys[0].Should().NotBe(keys[1]);
    }
}