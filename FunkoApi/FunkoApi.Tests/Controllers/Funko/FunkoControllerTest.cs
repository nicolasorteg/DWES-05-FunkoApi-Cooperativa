using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Controller.Funko;
using FunkoApi.Dto.Categoria;
using FunkoApi.Dto.Funko;
using FunkoApi.Dto.Proveedor;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Services.Funkos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunkoApi.Tests.Controllers.Funko;

[TestFixture]
public class FunkoControllerTest
{
    private Mock<IFunkoService> _funkoServiceMock = null!;
    private FunkoController _controller = null!;

    private static ResponseFunkoDto CrearFunkoResponse(long id = 1, string nombre = "Goku Super Saiyan") =>
        new(
            Id: id,
            Nombre: nombre,
            Precio: 19.99m,
            Stock: 15,
            Imagen: "goku.jpg",
            ResponseCategoria: new ResponseCategoriaDto("Anime00001", "Anime"),
            ResponseProveedor: new ResponseProveedorDto("65f1a2b3c4d5e6f7a8b9c0d1", "Bandai España", "contacto@bandai.es", "600112233"),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null
        );

    [SetUp]
    public void Setup()
    {
        _funkoServiceMock = new Mock<IFunkoService>();
        _controller = new FunkoController(_funkoServiceMock.Object);
    }

    [Test]
    public async Task GetAll_DevuelveOkConListaPaginada_CuandoServicioTieneExito()
    {
        IReadOnlyList<ResponseFunkoDto> funkos = new List<ResponseFunkoDto>
        {
            CrearFunkoResponse(1, "Goku"),
            CrearFunkoResponse(2, "Vegeta")
        };

        _funkoServiceMock
            .Setup(s => s.GetAllPagedAsync(1, 10))
            .ReturnsAsync(Result.Success<IReadOnlyList<ResponseFunkoDto>, DomainError>(funkos));

        var actionResult = await _controller.GetAll(1, 10);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(funkos);
        _funkoServiceMock.Verify(s => s.GetAllPagedAsync(1, 10), Times.Once);
    }

    [Test]
    public async Task GetAll_DevuelveBadRequest_CuandoPaginacionEsInvalida()
    {
        _funkoServiceMock
            .Setup(s => s.GetAllPagedAsync(-1, 10))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ResponseFunkoDto>, DomainError>(
                ModelErrors.Validation("Parámetros de paginación inválidos")));

        var actionResult = await _controller.GetAll(-1, 10);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task GetById_DevuelveOkConFunko_CuandoExiste()
    {
        var expected = CrearFunkoResponse(1, "Naruto");

        _funkoServiceMock
            .Setup(s => s.GetByIdAsync(1L))
            .ReturnsAsync(Result.Success<ResponseFunkoDto, DomainError>(expected));

        var actionResult = await _controller.GetById(1);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task GetById_DevuelveNotFound_CuandoNoExiste()
    {
        _funkoServiceMock
            .Setup(s => s.GetByIdAsync(999L))
            .ReturnsAsync(Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.NotFound("999")));

        var actionResult = await _controller.GetById(999);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Create_DevuelveCreatedAtAction_CuandoFunkoEsValido()
    {
        var dto = new CreateFunkoDto("Luffy Gear 5", 24.99m, 10, "Anime00001", "65f1a2b3c4d5e6f7a8b9c0d1", "luffy.jpg");
        var created = CrearFunkoResponse(5, "Luffy Gear 5");

        _funkoServiceMock
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(Result.Success<ResponseFunkoDto, DomainError>(created));

        var actionResult = await _controller.Create(dto);

        var createdResult = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(FunkoController.GetById));
        createdResult.RouteValues!["id"].Should().Be(5L);
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    [Test]
    public async Task Create_DevuelveBadRequest_CuandoValidacionFalla()
    {
        var dto = new CreateFunkoDto("", -10m, -1, "Anime00001", "65f1a2b3c4d5e6f7a8b9c0d1");

        _funkoServiceMock
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(Result.Failure<ResponseFunkoDto, DomainError>(
                ModelErrors.Validation("Datos de Funko inválidos")));

        var actionResult = await _controller.Create(dto);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task Create_DevuelveNotFound_CuandoCategoriaOProveedorNoExisten()
    {
        var dto = new CreateFunkoDto("Zoro", 18.50m, 5, "CatInexistente", "65f1a2b3c4d5e6f7a8b9c0d1");

        _funkoServiceMock
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(Result.Failure<ResponseFunkoDto, DomainError>(
                ModelErrors.CategoriaNotFound("CatInexistente")));

        var actionResult = await _controller.Create(dto);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Update_DevuelveOkConFunkoActualizado_CuandoTieneExito()
    {
        var dto = new UpdateFunkoDto("Goku Ultra Instinto", 29.99m, 8, "Anime00001", "65f1a2b3c4d5e6f7a8b9c0d1", "goku_ui.jpg");
        var updated = CrearFunkoResponse(1, "Goku Ultra Instinto");

        _funkoServiceMock
            .Setup(s => s.UpdateAsync(1L, dto))
            .ReturnsAsync(Result.Success<ResponseFunkoDto, DomainError>(updated));

        var actionResult = await _controller.Update(1, dto);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(updated);
    }

    [Test]
    public async Task Update_DevuelveNotFound_CuandoFunkoNoExiste()
    {
        var dto = new UpdateFunkoDto("Goku UI", 29.99m, 8, "Anime00001", "65f1a2b3c4d5e6f7a8b9c0d1");

        _funkoServiceMock
            .Setup(s => s.UpdateAsync(999L, dto))
            .ReturnsAsync(Result.Failure<ResponseFunkoDto, DomainError>(ModelErrors.NotFound("999")));

        var actionResult = await _controller.Update(999, dto);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Update_DevuelveBadRequest_CuandoDtoEsInvalido()
    {
        var dto = new UpdateFunkoDto("", 0m, -5, "Anime00001", "65f1a2b3c4d5e6f7a8b9c0d1");

        _funkoServiceMock
            .Setup(s => s.UpdateAsync(1L, dto))
            .ReturnsAsync(Result.Failure<ResponseFunkoDto, DomainError>(
                ModelErrors.Validation("Precio inválido")));

        var actionResult = await _controller.Update(1, dto);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task Delete_DevuelveNoContent_CuandoSeEliminaCorrectamente()
    {
        _funkoServiceMock
            .Setup(s => s.DeleteAsync(1L))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var actionResult = await _controller.Delete(1);

        var noContentResult = actionResult.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Test]
    public async Task Delete_DevuelveNotFound_CuandoFunkoNoExiste()
    {
        _funkoServiceMock
            .Setup(s => s.DeleteAsync(999L))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ModelErrors.NotFound("999")));

        var actionResult = await _controller.Delete(999);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}