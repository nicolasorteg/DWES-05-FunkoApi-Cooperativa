using FunkoApi.Dto.Proveedor;
using FunkoApi.Services.Common;

namespace FunkoApi.Services.Proveedor;

public interface IProveedorService : 
    ICrudService<ResponseProveedorDto, CreateProveedorDto, UpdateProveedorDto, string> { }