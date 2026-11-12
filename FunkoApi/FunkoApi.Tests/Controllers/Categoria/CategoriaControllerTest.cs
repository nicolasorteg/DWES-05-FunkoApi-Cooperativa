using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Controller.Categoria;
using FunkoApi.Dto.Categoria;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Services.Categoria;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunkoApi.Tests.Controllers.Categoria;

[TestFixture]
public class CategoriaControllerTest
{
    private Mock<ICategoriaService> _categoriaServiceMock = null!;
    private CategoriaController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _categoriaServiceMock = new Mock<ICategoriaService>();
        _controller = new CategoriaController(_categoriaServiceMock.Object);
    }

    [Test]
    public async Task GetAll_DevuelveOkConListaPaginada_CuandoServicioTieneExito()
    {
        IReadOnlyList<ResponseCategoriaDto> categorias = new List<ResponseCategoriaDto>
        {
            new("Anime00001", "Anime"),
            new("Marvel0001", "Marvel")
        };

        _categoriaServiceMock
            .Setup(s => s.GetAllPagedAsync(1, 10))
            .ReturnsAsync(Result.Success<IReadOnlyList<ResponseCategoriaDto>, DomainError>(categorias));

        var actionResult = await _controller.GetAll(1, 10);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(categorias);
        _categoriaServiceMock.Verify(s => s.GetAllPagedAsync(1, 10), Times.Once);
    }

    [Test]
    public async Task GetAll_DevuelveBadRequest_CuandoParametrosPaginacionSonInvalidos()
    {
        _categoriaServiceMock
            .Setup(s => s.GetAllPagedAsync(0, -5))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ResponseCategoriaDto>, DomainError>(
                ModelErrors.Validation("La página debe ser mayor que 0")));

        var actionResult = await _controller.GetAll(0, -5);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task GetById_DevuelveOkConCategoria_CuandoExiste()
    {
        var expected = new ResponseCategoriaDto("Anime00001", "Anime");

        _categoriaServiceMock
            .Setup(s => s.GetByIdAsync("Anime00001"))
            .ReturnsAsync(Result.Success<ResponseCategoriaDto, DomainError>(expected));

        var actionResult = await _controller.GetById("Anime00001");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task GetById_DevuelveNotFound_CuandoNoExiste()
    {
        _categoriaServiceMock
            .Setup(s => s.GetByIdAsync("Inexistente"))
            .ReturnsAsync(Result.Failure<ResponseCategoriaDto, DomainError>(ModelErrors.NotFound("Inexistente")));

        var actionResult = await _controller.GetById("Inexistente");

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Create_DevuelveCreatedAtAction_CuandoEsValido()
    {
        var dto = new CreateCategoriaDto("Videojuegos");
        var created = new ResponseCategoriaDto("Video00001", "Videojuegos");

        _categoriaServiceMock
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(Result.Success<ResponseCategoriaDto, DomainError>(created));

        var actionResult = await _controller.Create(dto);

        var createdResult = actionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(CategoriaController.GetById));
        createdResult.RouteValues!["id"].Should().Be("Video00001");
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    [Test]
    public async Task Create_DevuelveBadRequest_CuandoValidacionFalla()
    {
        var dto = new CreateCategoriaDto("");

        _categoriaServiceMock
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(Result.Failure<ResponseCategoriaDto, DomainError>(
                ModelErrors.Validation("El nombre es obligatorio")));

        var actionResult = await _controller.Create(dto);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task Update_DevuelveOkConCategoriaActualizada_CuandoTieneExito()
    {
        var dto = new UpdateCategoriaDto("Anime Editado");
        var updated = new ResponseCategoriaDto("Anime00001", "Anime Editado");

        _categoriaServiceMock
            .Setup(s => s.UpdateAsync("Anime00001", dto))
            .ReturnsAsync(Result.Success<ResponseCategoriaDto, DomainError>(updated));

        var actionResult = await _controller.Update("Anime00001", dto);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(updated);
    }

    [Test]
    public async Task Update_DevuelveNotFound_CuandoCategoriaNoExiste()
    {
        var dto = new UpdateCategoriaDto("Anime Editado");

        _categoriaServiceMock
            .Setup(s => s.UpdateAsync("Inexistente", dto))
            .ReturnsAsync(Result.Failure<ResponseCategoriaDto, DomainError>(ModelErrors.NotFound("Inexistente")));

        var actionResult = await _controller.Update("Inexistente", dto);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Update_DevuelveBadRequest_CuandoDtoEsInvalido()
    {
        var dto = new UpdateCategoriaDto("");

        _categoriaServiceMock
            .Setup(s => s.UpdateAsync("Anime00001", dto))
            .ReturnsAsync(Result.Failure<ResponseCategoriaDto, DomainError>(
                ModelErrors.Validation("El nombre es obligatorio")));

        var actionResult = await _controller.Update("Anime00001", dto);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Test]
    public async Task Delete_DevuelveNoContent_CuandoSeEliminaCorrectamente()
    {
        _categoriaServiceMock
            .Setup(s => s.DeleteAsync("Anime00001"))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var actionResult = await _controller.Delete("Anime00001");

        var noContentResult = actionResult.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Test]
    public async Task Delete_DevuelveNotFound_CuandoCategoriaNoExiste()
    {
        _categoriaServiceMock
            .Setup(s => s.DeleteAsync("Inexistente"))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ModelErrors.NotFound("Inexistente")));

        var actionResult = await _controller.Delete("Inexistente");

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task Delete_DevuelveConflict_CuandoTieneFunkosAsociados()
    {
        _categoriaServiceMock
            .Setup(s => s.DeleteAsync("Anime00001"))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ModelErrors.FunkoAsociado()));

        var actionResult = await _controller.Delete("Anime00001");

        var conflictResult = actionResult.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }
}