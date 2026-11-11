using FunkoApi.Dto.Categoria;
using FunkoApi.Errors.Models;
using FunkoApi.Services.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controller.Categoria;

[ApiController]
[Route("api/[controller]")]
public class CategoriaController(ICategoriaService categoriaService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await categoriaService.GetAllPagedAsync(page, pageSize);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await categoriaService.GetByIdAsync(id);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    
    // operaciones solo de admin
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateCategoriaDto dto)
    {
        var result = await categoriaService.AddAsync(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.Error.ToHttpResult();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoriaDto dto)
    {
        var result = await categoriaService.UpdateAsync(id, dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Error.ToHttpResult();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")] 
    public async Task<IActionResult> Delete(string id)
    {
        var result = await categoriaService.DeleteAsync(id);
        return result.IsSuccess
            ? NoContent()
            : result.Error.ToHttpResult();
    }
}