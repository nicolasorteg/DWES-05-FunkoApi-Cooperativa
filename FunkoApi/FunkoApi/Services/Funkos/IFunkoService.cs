using FunkoApi.Dto.Funko;
using FunkoApi.Services.Common;

namespace FunkoApi.Services.Funkos;

public interface IFunkoService : ICrudService<ResponseFunkoDto, CreateFunkoDto, UpdateFunkoDto, long>
{
    
}