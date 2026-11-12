using FunkoApi.Dto.Funko;
using FunkoApi.Services.Funkos;

namespace FunkoApi.GraphQL;


public class Query
{
    public async Task<IReadOnlyList<ResponseFunkoDto>> GetFunkos(
        [Service] IFunkoService funkoService,
        int page = 1,
        int pageSize = 10)
    {
        var result = await funkoService.GetAllPagedAsync(page, pageSize);
        return result.IsSuccess ? result.Value : [];
    }

    public async Task<ResponseFunkoDto?> GetFunkoById(
        int id,
        [Service] IFunkoService funkoService)
    {
        var result = await funkoService.GetByIdAsync(id);
        return result.IsSuccess ? result.Value : null;
    }
}