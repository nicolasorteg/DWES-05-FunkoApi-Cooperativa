using FunkoApi.Dto.Proveedor;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Models;
using FunkoApi.Services.Proveedor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controller.Proveedor;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProveedorController(IProveedorService proveedorService) : ControllerBase
{
    /// <summary>
    /// Obtiene la lista paginada de proveedores.
    /// </summary>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 10)</param>
    /// <returns>Lista de proveedores devueltos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResponseProveedorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await proveedorService.GetAllPagedAsync(page, pageSize);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Obtiene un proveedor específico por su identificador (ObjectId de MongoDB).
    /// </summary>
    /// <param name="id">Identificador único del proveedor en MongoDB</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await proveedorService.GetByIdAsync(id);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Crea un nuevo proveedor. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="dto">Datos para la creación del proveedor</param>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseProveedorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProveedorDto dto)
    {
        var result = await proveedorService.AddAsync(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Actualiza un proveedor existente. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="id">Identificador del proveedor a actualizar</param>
    /// <param name="dto">Nuevos datos del proveedor</param>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProveedorDto dto)
    {
        var result = await proveedorService.UpdateAsync(id, dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    /// <summary>
    /// Elimina un proveedor. (Requiere rol ADMIN)
    /// </summary>
    /// <param name="id">Identificador del proveedor a eliminar</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await proveedorService.DeleteAsync(id);
        return result.IsSuccess
            ? NoContent()
            : result.Error.ToHttpResult();
    }
}