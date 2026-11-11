using FunkoApi.Dto.Funko;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Services.Funkos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controller.Funko;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FunkoController(IFunkoService funkoService) : ControllerBase
{
    /// <summary>
    /// Obtiene la lista paginada de Funkos del catálogo.
    /// </summary>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 10)</param>
    /// <returns>Lista paginada de Funkos devueltos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResponseFunkoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await funkoService.GetAllPagedAsync(page, pageSize);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Obtiene un Funko específico por su identificador único.
    /// </summary>
    /// <param name="id">Identificador numérico del Funko</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResponseFunkoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await funkoService.GetByIdAsync(id);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Crea un nuevo Funko en el catálogo. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="dto">Datos para la creación del Funko</param>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseFunkoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateFunkoDto dto)
    {
        var result = await funkoService.AddAsync(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Actualiza los datos de un Funko existente. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="id">Identificador del Funko a actualizar</param>
    /// <param name="dto">Nuevos datos del Funko</param>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseFunkoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFunkoDto dto)
    {
        var result = await funkoService.UpdateAsync(id, dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Elimina (borrado lógico) un Funko del catálogo. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="id">Identificador del Funko a eliminar</param>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await funkoService.DeleteAsync(id);
        return result.IsSuccess
            ? NoContent()
            : result.Error.ToHttpResult();
    }
}