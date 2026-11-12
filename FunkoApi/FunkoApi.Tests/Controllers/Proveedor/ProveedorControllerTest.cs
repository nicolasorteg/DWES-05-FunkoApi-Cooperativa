using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Controller.Proveedor;
using FunkoApi.Dto.Proveedor;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Services.Proveedor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunkoApi.Tests.Controllers.Proveedor;

[TestFixture]
public class ProveedorControllerTest
{
    private Mock<IProveedorService> _proveedorServiceMock = null!;
    private ProveedorController _controller = null!;

    private const string ValidObjectId = "65f1a2b3c4d5e6f7a8b9c0d1";

    [SetUp]
    public void Setup()
    {
        _proveedorServiceMock = new Mock<IProveedorService>();
        _controller = new ProveedorController(_proveedorServiceMock.Object);
    }

    [Test]
    public async Task GetAll_DevuelveOkConListaPaginada_CuandoServicioTieneExito()
    {
        IReadOnlyList<ResponseProveedorDto> proveedores = new List<ResponseProveedorDto>
        {
            new(ValidObjectId, "Bandai España", "contacto@bandai.es", "600112233"),
            new("65f1a2b3c4d5e6f7a8b9c0d2", "Funko Europe", "sales@funkoeurope.com", null)
        };

        _proveedorServiceMock
            .Setup(s => s.GetAllPagedAsync(1, 10))
            .ReturnsAsync(Result.Success<IReadOnlyList<ResponseProveedorDto>, DomainError>(proveedores));

        var actionResult = await _controller.GetAll(1, 10);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(proveedores);
        _proveedorServiceMock.Verify(s => s.GetAllPagedAsync(1, 10), Times.Once);
    }

    [Test]
    public async Task GetAll_DevuelveBadRequest_CuandoPaginacionEsInvalida()
    {
        _proveedorServiceMock
            .Setup(s => s.GetAllPagedAsync(0, 10))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ResponseProveedorDto>, DomainError>(
                ModelErrors.Validation("Página inválida")));

        var actionResult = await _controller.GetAll(0, 10);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task GetById_DevuelveOkConProveedor_CuandoExiste()
    {
        var expected = new ResponseProveedorDto(ValidObjectId, "Bandai España", "contacto@bandai.es", "600112233");

        _proveedorServiceMock
            .Setup(s => s.GetByIdAsync(ValidObjectId))
            .ReturnsAsync(Result.Success<ResponseProveedorDto, DomainError>(expected));

        var actionResult = await _controller.GetById(ValidObjectId);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task GetById_DevuelveNotFound_CuandoNoExiste()
    {
        _proveedorServiceMock
            .Setup(s => s.GetByIdAsync("id-inexistente"))
            .ReturnsAsync(Result.Failure<ResponseProveedorDto, DomainError>(ModelErrors.NotFound("id-inexistente")));

        var actionResult = await _controller.GetById("id-inexistente");

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Create_DevuelveCreatedAtAction_CuandoEsValido()
    {
        var dto = new CreateProveedorDto("Distribución Madrid", "info@distmadrid.es", "910000000");
        var created = new ResponseProveedorDto(ValidObjectId, "Distribución Madrid", "info@distmadrid.es", "910000000");

        _proveedorServiceMock
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(Result.Success<ResponseProveedorDto, DomainError>(created));

        var actionResult = await _controller.Create(dto);

        var createdResult = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(ProveedorController.GetById));
        createdResult.RouteValues!["id"].Should().Be(ValidObjectId);
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    [Test]
    public async Task Create_DevuelveBadRequest_CuandoValidacionFalla()
    {
        var dto = new CreateProveedorDto("admin", "email-no-valido", null);

        _proveedorServiceMock
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(Result.Failure<ResponseProveedorDto, DomainError>(
                ModelErrors.Validation("El nombre no puede contener admin")));

        var actionResult = await _controller.Create(dto);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task Update_DevuelveOkConProveedorActualizado_CuandoTieneExito()
    {
        var dto = new UpdateProveedorDto("Bandai Ibérica", "nuevo@bandai.es", "600998877");
        var updated = new ResponseProveedorDto(ValidObjectId, "Bandai Ibérica", "nuevo@bandai.es", "600998877");

        _proveedorServiceMock
            .Setup(s => s.UpdateAsync(ValidObjectId, dto))
            .ReturnsAsync(Result.Success<ResponseProveedorDto, DomainError>(updated));

        var actionResult = await _controller.Update(ValidObjectId, dto);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(updated);
    }

    [Test]
    public async Task Update_DevuelveNotFound_CuandoProveedorNoExiste()
    {
        var dto = new UpdateProveedorDto("Bandai Ibérica", "nuevo@bandai.es", "600998877");

        _proveedorServiceMock
            .Setup(s => s.UpdateAsync("id-inexistente", dto))
            .ReturnsAsync(Result.Failure<ResponseProveedorDto, DomainError>(ModelErrors.NotFound("id-inexistente")));

        var actionResult = await _controller.Update("id-inexistente", dto);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Update_DevuelveBadRequest_CuandoDtoEsInvalido()
    {
        var dto = new UpdateProveedorDto("", "mal-email", "123");

        _proveedorServiceMock
            .Setup(s => s.UpdateAsync(ValidObjectId, dto))
            .ReturnsAsync(Result.Failure<ResponseProveedorDto, DomainError>(
                ModelErrors.Validation("Datos de proveedor inválidos")));

        var actionResult = await _controller.Update(ValidObjectId, dto);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task Delete_DevuelveNoContent_CuandoSeEliminaCorrectamente()
    {
        _proveedorServiceMock
            .Setup(s => s.DeleteAsync(ValidObjectId))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var actionResult = await _controller.Delete(ValidObjectId);

        var noContentResult = actionResult.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Test]
    public async Task Delete_DevuelveNotFound_CuandoProveedorNoExiste()
    {
        _proveedorServiceMock
            .Setup(s => s.DeleteAsync("id-inexistente"))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ModelErrors.NotFound("id-inexistente")));

        var actionResult = await _controller.Delete("id-inexistente");

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Delete_DevuelveConflict_CuandoTieneFunkosAsociados()
    {
        _proveedorServiceMock
            .Setup(s => s.DeleteAsync(ValidObjectId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ModelErrors.FunkoAsociado()));

        var actionResult = await _controller.Delete(ValidObjectId);

        var conflictResult = actionResult.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }
}