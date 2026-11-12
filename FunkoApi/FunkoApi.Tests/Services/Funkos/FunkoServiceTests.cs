using FluentAssertions;
using FluentValidation.Results;
using Moq;
using FunkoApi.Dto.Funko;
using FunkoApi.Errors.Models;
using FunkoApi.Hubs;
using FunkoApi.Mapper;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Repositories.Proveedores;
using FunkoApi.Services.Cache;
using FunkoApi.Services.Funkos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace FunkoApi.Tests.Services.Funkos;

[TestFixture]
public class FunkoServiceTests
{
    private const string CategoriaId = "Anime00001";

    // helpers compartidos por los fixtures anidados
    private static Categoria CrearCategoria() => new() { Id = CategoriaId, Nombre = "Anime" };

    private static Proveedor CrearProveedor() => new()
    {
        Id = ObjectId.GenerateNewId(),
        Nombre = "Bandai",
        Email = "info@bandai.com"
    };

    private static Funko CrearFunko(string proveedorId, long id = 1) => new()
    {
        Id = id,
        Nombre = "Goku",
        Precio = 19.99m,
        Stock = 5,
        CategoriaId = CategoriaId,
        Categoria = CrearCategoria(),
        ProveedorId = proveedorId,
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static CreateFunkoDto CrearCreateDto(string proveedorId) =>
        new("Goku", 19.99m, 5, CategoriaId, proveedorId, null);

    private static UpdateFunkoDto CrearUpdateDto(string proveedorId) =>
        new("Goku Editado", 24.99m, 10, CategoriaId, proveedorId, null);

    // mocks y servicio compartidos: NUnit no comparte la instancia del fixture externo
    // con los fixtures anidados, por eso son estáticos y se reinicializan en cada test
    private static Mock<IFunkosRepository> _funkoRepoMock = null!;
    private static Mock<ICategoriaRepository> _categoriaRepoMock = null!;
    private static Mock<IProveedorRepository> _proveedorRepoMock = null!;
    private static Mock<IValidator<CreateFunkoDto>> _createValidatorMock = null!;
    private static Mock<IValidator<UpdateFunkoDto>> _updateValidatorMock = null!;
    private static Mock<ICacheService> _cacheMock = null!;
    private static Mock<IHubContext<FunkosHub>> _funkosHubMock = null!;
    private static Mock<IClientProxy> _clientProxyMock = null!;

    private static FunkoService _service = null!;

    private static void Inicializar()
    {
        _funkoRepoMock = new Mock<IFunkosRepository>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _proveedorRepoMock = new Mock<IProveedorRepository>();
        _createValidatorMock = new Mock<IValidator<CreateFunkoDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateFunkoDto>>();
        _cacheMock = new Mock<ICacheService>();
        _funkosHubMock = new Mock<IHubContext<FunkosHub>>();
        
        _clientProxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _funkosHubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        // por defecto los DTOs son válidos; los tests de CasosInvalidos lo sobrescriben
        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateFunkoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateFunkoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _service = new FunkoService(
            _funkoRepoMock.Object,
            _categoriaRepoMock.Object,
            _proveedorRepoMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _cacheMock.Object,
            _funkosHubMock.Object,
            new Mock<ILogger<FunkoService>>().Object
        );
    }

    [TestFixture]
    public class CasosValidos
    {
        [SetUp]
        public void Setup() => Inicializar();

        [Test]
        public async Task GetAllPagedAsync_DevuelveFunkosConProveedor_CuandoTodoExiste()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var funko = CrearFunko(proveedorId);

            _funkoRepoMock
                .Setup(r => r.GetAllPagedAsync(1, 10))
                .ReturnsAsync(new List<Funko> { funko });
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);

            // Act
            var result = await _service.GetAllPagedAsync(1, 10);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].Nombre.Should().Be("Goku");
            result.Value[0].ResponseCategoria.Nombre.Should().Be("Anime");
            result.Value[0].ResponseProveedor.Nombre.Should().Be("Bandai");
        }

        [Test]
        public async Task GetAllPagedAsync_ConsultaProveedorUnaSolaVez_CuandoSeRepiteEnVariosFunkos()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var funkos = new List<Funko>
            {
                CrearFunko(proveedorId, id: 1),
                CrearFunko(proveedorId, id: 2)
            };

            _funkoRepoMock
                .Setup(r => r.GetAllPagedAsync(1, 10))
                .ReturnsAsync(funkos);
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);

            // Act
            var result = await _service.GetAllPagedAsync(1, 10);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            _proveedorRepoMock.Verify(
                r => r.GetByIdAsync(proveedorId),
                Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_DevuelveDesdeCache_CuandoExisteEnCache()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var funko = CrearFunko(proveedor.Id.ToString());
            var cached = funko.ToResponseDto(proveedor);

            _cacheMock
                .Setup(c => c.GetAsync<ResponseFunkoDto>("funko:1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cached);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeSameAs(cached);
            _funkoRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
        }

        [Test]
        public async Task GetByIdAsync_DevuelveDtoYGuardaEnCache_CuandoNoExisteEnCache()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var funko = CrearFunko(proveedorId);

            _cacheMock
                .Setup(c => c.GetAsync<ResponseFunkoDto>("funko:1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ResponseFunkoDto?)null);
            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(funko);
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(1);
            result.Value.Nombre.Should().Be("Goku");
            result.Value.ResponseProveedor.Nombre.Should().Be("Bandai");
            _cacheMock.Verify(
                c => c.SetAsync("funko:1", It.IsAny<ResponseFunkoDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task AddAsync_CreaFunko_CuandoCategoriaYProveedorExisten()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var dto = CrearCreateDto(proveedorId);
            var funkoCreado = CrearFunko(proveedorId);
            var funkoCompleto = CrearFunko(proveedorId);

            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync(CrearCategoria());
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);
            _funkoRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Funko>()))
                .ReturnsAsync(funkoCreado);
            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(funkoCompleto);

            // Act
            var result = await _service.AddAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Nombre.Should().Be("Goku");
            result.Value.ResponseProveedor.Nombre.Should().Be("Bandai");
            _funkoRepoMock.Verify(
                r => r.AddAsync(It.Is<Funko>(f =>
                    f.Nombre == "Goku" &&
                    f.Precio == 19.99m &&
                    f.Stock == 5 &&
                    f.CategoriaId == CategoriaId &&
                    f.ProveedorId == proveedorId)),
                Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ActualizaFunkoEInvalidaCache_CuandoTodoEsValido()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var existente = CrearFunko(proveedorId);
            var dto = CrearUpdateDto(proveedorId);

            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existente);
            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync(CrearCategoria());
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);
            _funkoRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Funko>()))
                .ReturnsAsync(existente);

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Nombre.Should().Be("Goku Editado");
            result.Value.Stock.Should().Be(10);
            _cacheMock.Verify(
                c => c.RemoveAsync("funko:1", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task DeleteAsync_BorraFunkoEInvalidaCache_CuandoExiste()
        {
            // Arrange
            _funkoRepoMock
                .Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _cacheMock.Verify(
                c => c.RemoveAsync("funko:1", It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [SetUp]
        public void Setup() => Inicializar();

        [Test]
        public async Task GetAllPagedAsync_DevuelveProveedorNotFound_CuandoUnProveedorNoExiste()
        {
            // Arrange
            var funko = CrearFunko("prov-inexistente");

            _funkoRepoMock
                .Setup(r => r.GetAllPagedAsync(1, 10))
                .ReturnsAsync(new List<Funko> { funko });
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync("prov-inexistente"))
                .ReturnsAsync((Proveedor?)null);

            // Act
            var result = await _service.GetAllPagedAsync(1, 10);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.ProveedorNotFound("prov-inexistente"));
        }

        [Test]
        public async Task GetByIdAsync_DevuelveNotFound_CuandoNoExisteEnRepo()
        {
            // Arrange
            _cacheMock
                .Setup(c => c.GetAsync<ResponseFunkoDto>("funko:1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ResponseFunkoDto?)null);
            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Funko?)null);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.NotFound("1"));
            _cacheMock.Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<ResponseFunkoDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task GetByIdAsync_DevuelveProveedorNotFound_CuandoElProveedorDelFunkoNoExiste()
        {
            // Arrange
            var proveedorId = ObjectId.GenerateNewId().ToString();
            var funko = CrearFunko(proveedorId);

            _cacheMock
                .Setup(c => c.GetAsync<ResponseFunkoDto>("funko:1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ResponseFunkoDto?)null);
            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(funko);
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync((Proveedor?)null);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.ProveedorNotFound(proveedorId));
            _cacheMock.Verify(
                c => c.SetAsync(It.IsAny<string>(), It.IsAny<ResponseFunkoDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task AddAsync_DevuelveErrorValidacion_CuandoDtoEsInvalido()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var dto = CrearCreateDto(proveedor.Id.ToString());

            _createValidatorMock
                .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Nombre", "El nombre del Funko es obligatorio.") }));

            // Act
            var result = await _service.AddAsync(dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.Validation("El nombre del Funko es obligatorio."));
            _categoriaRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _funkoRepoMock.Verify(r => r.AddAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task AddAsync_DevuelveCategoriaNotFound_CuandoLaCategoriaNoExiste()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var dto = CrearCreateDto(proveedorId);

            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync((Categoria?)null);

            // Act
            var result = await _service.AddAsync(dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.CategoriaNotFound(CategoriaId));
            _funkoRepoMock.Verify(r => r.AddAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task AddAsync_DevuelveProveedorNotFound_CuandoElProveedorNoExiste()
        {
            // Arrange
            var proveedorId = ObjectId.GenerateNewId().ToString();
            var dto = CrearCreateDto(proveedorId);

            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync(CrearCategoria());
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync((Proveedor?)null);

            // Act
            var result = await _service.AddAsync(dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.ProveedorNotFound(proveedorId));
            _funkoRepoMock.Verify(r => r.AddAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task AddAsync_DevuelveProveedorNotFound_CuandoElProveedorEstaBorrado()
        {
            // Arrange
            var proveedor = CrearProveedor();
            proveedor.IsDeleted = true;
            var proveedorId = proveedor.Id.ToString();
            var dto = CrearCreateDto(proveedorId);

            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync(CrearCategoria());
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);

            // Act
            var result = await _service.AddAsync(dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.ProveedorNotFound(proveedorId));
            _funkoRepoMock.Verify(r => r.AddAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_DevuelveErrorValidacion_CuandoDtoEsInvalido()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var dto = CrearUpdateDto(proveedor.Id.ToString());

            _updateValidatorMock
                .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Precio", "El precio debe ser mayor que 0.") }));

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.Validation("El precio debe ser mayor que 0."));
            _funkoRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
            _funkoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_DevuelveNotFound_CuandoElFunkoNoExiste()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var dto = CrearUpdateDto(proveedorId);

            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Funko?)null);

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.NotFound("1"));
            _funkoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_DevuelveCategoriaNotFound_CuandoLaCategoriaNoExiste()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var existente = CrearFunko(proveedorId);
            var dto = CrearUpdateDto(proveedorId);

            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existente);
            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync((Categoria?)null);

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.CategoriaNotFound(CategoriaId));
            _funkoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_DevuelveProveedorNotFound_CuandoElProveedorNoExiste()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var existente = CrearFunko(proveedorId);
            var dto = CrearUpdateDto(proveedorId);

            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existente);
            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync(CrearCategoria());
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync((Proveedor?)null);

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.ProveedorNotFound(proveedorId));
            _funkoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_DevuelveProveedorNotFound_CuandoElProveedorEstaBorrado()
        {
            // Arrange
            var proveedor = CrearProveedor();
            proveedor.IsDeleted = true;
            var proveedorId = proveedor.Id.ToString();
            var existente = CrearFunko(proveedorId);
            var dto = CrearUpdateDto(proveedorId);

            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existente);
            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync(CrearCategoria());
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.ProveedorNotFound(proveedorId));
            _funkoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_DevuelveNotFound_CuandoElRepoDevuelveNullAlActualizar()
        {
            // Arrange
            var proveedor = CrearProveedor();
            var proveedorId = proveedor.Id.ToString();
            var existente = CrearFunko(proveedorId);
            var dto = CrearUpdateDto(proveedorId);

            _funkoRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existente);
            _categoriaRepoMock
                .Setup(r => r.GetByIdAsync(CategoriaId))
                .ReturnsAsync(CrearCategoria());
            _proveedorRepoMock
                .Setup(r => r.GetByIdAsync(proveedorId))
                .ReturnsAsync(proveedor);
            _funkoRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Funko>()))
                .ReturnsAsync((Funko?)null);

            // Act
            var result = await _service.UpdateAsync(1, dto);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.NotFound("1"));
            _cacheMock.Verify(
                c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task DeleteAsync_DevuelveNotFound_CuandoElFunkoNoExiste()
        {
            // Arrange
            _funkoRepoMock
                .Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeEquivalentTo(ModelErrors.NotFound("1"));
            _cacheMock.Verify(
                c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}