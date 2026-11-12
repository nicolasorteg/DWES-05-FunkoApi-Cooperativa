using FunkoApi.Dto.Funko;
using FunkoApi.Services.Funkos;
using HotChocolate.Authorization;

namespace FunkoApi.GraphQL;

public class Mutation
{
    [Authorize(Roles = new[] { "ADMIN" })]
    public async Task<ResponseFunkoDto?> CrearFunko(
        CreateFunkoDto input,
        [Service] IFunkoService funkoService)
    {
        var result = await funkoService.AddAsync(input);
        return result.IsSuccess ? result.Value : null;
    }

    [Authorize(Roles = new[] { "ADMIN" })]
    public async Task<bool> EliminarFunko(
        int id,
        [Service] IFunkoService funkoService)
    {
        var result = await funkoService.DeleteAsync(id);
        return result.IsSuccess;
    }
}