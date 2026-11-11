using FunkoApi.Dto.Categoria;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Services.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controller.Categoria;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriaController(ICategoriaService categoriaService) : ControllerBase
{
    /// <summary>
    /// Obtiene la lista paginada de categorías.
    /// </summary>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 10)</param>
    /// <returns>Lista de categorías devueltas</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResponseCategoriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await categoriaService.GetAllPagedAsync(page, pageSize);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Obtiene una categoría específica por su identificador.
    /// </summary>
    /// <param name="id">Identificador único de la categoría</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseCategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await categoriaService.GetByIdAsync(id);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Crea una nueva categoría. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="dto">Datos para la creación de la categoría</param>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseCategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCategoriaDto dto)
    {
        var result = await categoriaService.AddAsync(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Actualiza una categoría existente. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="id">Identificador de la categoría a actualizar</param>
    /// <param name="dto">Nuevos datos de la categoría</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseCategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoriaDto dto)
    {
        var result = await categoriaService.UpdateAsync(id, dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Elimina una categoría. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="id">Identificador de la categoría a eliminar</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await categoriaService.DeleteAsync(id);
        return result.IsSuccess
            ? NoContent()
            : result.Error.ToHttpResult();
    }
}