using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using FunkoApi.Dto.Categoria;
using FunkoApi.Errors.Models;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Services.Cache;
using FunkoApi.Services.Categoria;

namespace FunkoApi.Tests.Services.Categorias;

[TestFixture]
public class CategoriaServiceTests
{
    private Mock<ICategoriaRepository> _categoriaRepoMock = null!;
    private Mock<IFunkosRepository> _funkoRepoMock = null!;
    private Mock<IValidator<CreateCategoriaDto>> _createValidatorMock = null!;
    private Mock<IValidator<UpdateCategoriaDto>> _updateValidatorMock = null!;
    private Mock<ICacheService> _cacheMock = null!;

    private CategoriaService _service = null!;

    [SetUp]
    public void Setup()
    {
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _funkoRepoMock = new Mock<IFunkosRepository>();
        _createValidatorMock = new Mock<IValidator<CreateCategoriaDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateCategoriaDto>>();
        _cacheMock = new Mock<ICacheService>();

        _service = new CategoriaService(
            _categoriaRepoMock.Object,
            _funkoRepoMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _cacheMock.Object
        );
    }
    
    private List<string> CapturarClavesPaginadas()
    {
        var keys = new List<string>();
        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ResponseCategoriaDto>>(It.IsAny<string>()))
            .Callback<string, CancellationToken>((k, _) => keys.Add(k))
            .ReturnsAsync((IReadOnlyList<ResponseCategoriaDto>?)null);
        _categoriaRepoMock
            .Setup(r => r.GetAllPagedAsync(1, 10))
            .ReturnsAsync(new List<Categoria>());
        return keys;
    }

    [Test]
    public async Task GetAllPagedAsync_DevuelveDesdeCache_CuandoExisteEnCache()
    {
        var cachedList = new List<ResponseCategoriaDto> { new("Anime00001", "Anime") };

        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ResponseCategoriaDto>>(
                It.Is<string>(k => k.StartsWith("categorias:paged:"))))
            .ReturnsAsync(cachedList);

        var result = await _service.GetAllPagedAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Nombre.Should().Be("Anime");
        _categoriaRepoMock.Verify(r => r.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetAllPagedAsync_ConsultaRepoYGuardaEnCache_CuandoNoExisteEnCache()
    {
        _cacheMock
            .Setup(c => c.GetAsync<IReadOnlyList<ResponseCategoriaDto>>(It.IsAny<string>()))
            .ReturnsAsync((IReadOnlyList<ResponseCategoriaDto>?)null);

        _categoriaRepoMock
            .Setup(r => r.GetAllPagedAsync(1, 10))
            .ReturnsAsync(new List<Categoria> { new() { Id = "Anime00001", Nombre = "Anime" } });

        var result = await _service.GetAllPagedAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _cacheMock.Verify(
            c => c.SetAsync(
                It.Is<string>(k => k.StartsWith("categorias:paged:") && k.EndsWith(":1:10")),
                It.IsAny<List<ResponseCategoriaDto>>(),
                It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_DevuelveDesdeCache_CuandoExisteEnCache()
    {
        var cached = new ResponseCategoriaDto("Anime00001", "Anime");
        _cacheMock
            .Setup(c => c.GetAsync<ResponseCategoriaDto>("categoria:Anime00001"))
            .ReturnsAsync(cached);

        var result = await _service.GetByIdAsync("Anime00001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Anime");
        _categoriaRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_DevuelveNotFound_CuandoNoExisteEnRepo()
    {
        _cacheMock
            .Setup(c => c.GetAsync<ResponseCategoriaDto>("categoria:Inexistente"))
            .ReturnsAsync((ResponseCategoriaDto?)null);

        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Inexistente"))
            .ReturnsAsync((Categoria?)null);

        var result = await _service.GetByIdAsync("Inexistente");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound("Inexistente"));
    }

    [Test]
    public async Task GetByIdAsync_DevuelveDtoYGuardaEnCache_CuandoExisteEnRepo()
    {
        _cacheMock
            .Setup(c => c.GetAsync<ResponseCategoriaDto>("categoria:Anime00001"))
            .ReturnsAsync((ResponseCategoriaDto?)null);

        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Anime00001"))
            .ReturnsAsync(new Categoria { Id = "Anime00001", Nombre = "Anime" });

        var result = await _service.GetByIdAsync("Anime00001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("Anime00001");
        _cacheMock.Verify(
            c => c.SetAsync("categoria:Anime00001", It.IsAny<ResponseCategoriaDto>(), It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Test]
    public async Task AddAsync_DevuelveErrorValidacion_CuandoDtoEsInvalido()
    {
        var dto = new CreateCategoriaDto("");
        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Nombre", "El nombre es obligatorio") }));

        var result = await _service.AddAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.Validation("El nombre es obligatorio"));
        _categoriaRepoMock.Verify(r => r.AddAsync(It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task AddAsync_CreaCategoriaEInvalidaCachePaginada_CuandoDtoEsValido()
    {
        var keys = CapturarClavesPaginadas();
        var dto = new CreateCategoriaDto("Manga");
        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _categoriaRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(new Categoria { Id = "Manga00001", Nombre = "Manga" });

        await _service.GetAllPagedAsync(1, 10);  
        var result = await _service.AddAsync(dto);
        await _service.GetAllPagedAsync(1, 10); 

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Manga");
        keys.Should().HaveCount(2);
        keys[0].Should().NotBe(keys[1]);                 
    }
    

    [Test]
    public async Task UpdateAsync_DevuelveErrorValidacion_CuandoDtoEsInvalido()
    {
        var dto = new UpdateCategoriaDto("");
        _updateValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Nombre", "El nombre es obligatorio") }));

        var result = await _service.UpdateAsync("Anime00001", dto);

        result.IsFailure.Should().BeTrue();
        _categoriaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_DevuelveNotFound_CuandoCategoriaNoExiste()
    {
        var dto = new UpdateCategoriaDto("Nuevo Nombre");
        _updateValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Inexistente"))
            .ReturnsAsync((Categoria?)null);

        var result = await _service.UpdateAsync("Inexistente", dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound("Inexistente"));
    }

    [Test]
    public async Task UpdateAsync_DevuelveNotFound_CuandoElRepoDevuelveNullAlActualizar()
    {
        var dto = new UpdateCategoriaDto("Anime Editado");
        _updateValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Anime00001"))
            .ReturnsAsync(new Categoria { Id = "Anime00001", Nombre = "Anime" });
        _categoriaRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Categoria>()))
            .ReturnsAsync((Categoria?)null);

        var result = await _service.UpdateAsync("Anime00001", dto);

        result.IsFailure.Should().BeTrue();
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_ActualizaCategoriaEInvalidaCache_CuandoEsValido()
    {
        var keys = CapturarClavesPaginadas();
        var dto = new UpdateCategoriaDto("Anime Editado");
        _updateValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Anime00001"))
            .ReturnsAsync(new Categoria { Id = "Anime00001", Nombre = "Anime" });
        _categoriaRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(new Categoria { Id = "Anime00001", Nombre = "Anime Editado" });

        await _service.GetAllPagedAsync(1, 10);
        var result = await _service.UpdateAsync("Anime00001", dto);
        await _service.GetAllPagedAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Anime Editado");
        _cacheMock.Verify(c => c.RemoveAsync("categoria:Anime00001"), Times.Once);
        keys[0].Should().NotBe(keys[1]);
    }
    
    [Test]
    public async Task DeleteAsync_DevuelveNotFound_CuandoCategoriaNoExiste()
    {
        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Inexistente"))
            .ReturnsAsync((Categoria?)null);

        var result = await _service.DeleteAsync("Inexistente");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.NotFound("Inexistente"));
    }

    [Test]
    public async Task DeleteAsync_DevuelveErrorFunkoAsociado_CuandoTieneFunkosAsignados()
    {
        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Anime00001"))
            .ReturnsAsync(new Categoria { Id = "Anime00001", Nombre = "Anime" });
        _funkoRepoMock
            .Setup(r => r.ExistsByCategoriaAsync("Anime00001"))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync("Anime00001");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ModelErrors.FunkoAsociado());
        _categoriaRepoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_DevuelveNotFound_CuandoElRepoNoPuedeBorrar()
    {
        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Anime00001"))
            .ReturnsAsync(new Categoria { Id = "Anime00001", Nombre = "Anime" });
        _funkoRepoMock
            .Setup(r => r.ExistsByCategoriaAsync("Anime00001"))
            .ReturnsAsync(false);
        _categoriaRepoMock
            .Setup(r => r.DeleteAsync("Anime00001"))
            .ReturnsAsync(false);

        var result = await _service.DeleteAsync("Anime00001");

        result.IsFailure.Should().BeTrue();
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_EliminaEInvalidaCache_CuandoEsValidoYNoTieneFunkos()
    {
        var keys = CapturarClavesPaginadas();
        _categoriaRepoMock
            .Setup(r => r.GetByIdAsync("Anime00001"))
            .ReturnsAsync(new Categoria { Id = "Anime00001", Nombre = "Anime" });
        _funkoRepoMock.Setup(r => r.ExistsByCategoriaAsync("Anime00001")).ReturnsAsync(false);
        _categoriaRepoMock.Setup(r => r.DeleteAsync("Anime00001")).ReturnsAsync(true);

        await _service.GetAllPagedAsync(1, 10);
        var result = await _service.DeleteAsync("Anime00001");
        await _service.GetAllPagedAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(c => c.RemoveAsync("categoria:Anime00001"), Times.Once);
        keys[0].Should().NotBe(keys[1]);
    }
}