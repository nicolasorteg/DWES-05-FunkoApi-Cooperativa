using FluentAssertions;
using FunkoApi.Entity;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using FunkoApi.Repositories.Funkos;
using FunkoApi.Tests.Repositories.Categorias;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Tests.Repositories.Funkos;

[TestFixture]
[TestOf(typeof(FunkosRepository))]
public class FunkosRepositoryTest {

    // ObjectId de Mongo válido (24 caracteres): el proveedor no tiene FK en Postgres
    private const string ProveedorId = "507f1f77bcf86cd799439011";
    private const long IdInexistente = 999999;

    private static Funko NuevoFunko(string categoriaId, string nombre = "Spider-Man") => new() {
        Nombre = nombre,
        Precio = 19.99m,
        Stock = 10,
        CategoriaId = categoriaId,
        ProveedorId = ProveedorId
    };

    // inserta n funkos de golpe: "Funko 001", "Funko 002"...
    private static async Task SembrarAsync(AppDbContext context, string categoriaId, int n) {
        context.Funkos.AddRange(Enumerable.Range(1, n).Select(i => {
            var f = NuevoFunko(categoriaId, $"Funko {i:000}");
            f.CreatedAt = DateTime.UtcNow;
            return f;
        }));
        await context.SaveChangesAsync();
    }

    // devuelve un id que el repo NO debe encontrar: inexistente o de un funko ya borrado
    private static async Task<long> IdNoDisponibleAsync(IFunkosRepository repo, string categoriaId, bool borrado) {
        if (!borrado) return IdInexistente;

        var creado = await repo.AddAsync(NuevoFunko(categoriaId));
        await repo.DeleteAsync(creado.Id);
        return creado.Id;
    }

    [TestFixture]
    public class CasosValidos : PostgresTestBase {

        private AppDbContext _context = null!;
        private IFunkosRepository _repository = null!;
        private Categoria _categoria = null!;

        [SetUp]
        public async Task SetUp() {
            _context = NewContext();

            // BD limpia en cada test: primero funkos (FK Restrict) y después categorías
            await _context.Funkos.IgnoreQueryFilters().ExecuteDeleteAsync();
            await _context.Categorias.IgnoreQueryFilters().ExecuteDeleteAsync();

            _repository = new FunkosRepository(_context);

            // todo funko necesita una categoría existente por la FK
            _categoria = await new CategoriaRepository(_context).AddAsync(new Categoria { Nombre = "Marvel" });
        }

        [TearDown]
        public async Task TearDown() => await _context.DisposeAsync();

        [Test]
        public async Task AddAsync_FunkoValido_GeneraIdYRellenaCampos() {
            // arrange
            var entrada = NuevoFunko(_categoria.Id);
            entrada.IsDeleted = true; // el repo debe forzarlo a false

            // act
            var resultado = await _repository.AddAsync(entrada);

            // assert
            resultado.Id.Should().BePositive();
            resultado.IsDeleted.Should().BeFalse();
            resultado.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // se comprueba con otro contexto: viene de la BD, no del tracker
            await using var otro = NewContext();
            (await otro.Funkos.AnyAsync(f => f.Id == resultado.Id)).Should().BeTrue();
        }

        [Test]
        public async Task GetAllAsync_ConDatos_DevuelveNoBorradosOrdenadosPorNombre() {
            // arrange
            await _repository.AddAsync(NuevoFunko(_categoria.Id, "Venom"));
            await _repository.AddAsync(NuevoFunko(_categoria.Id, "Ant-Man"));
            var borrado = await _repository.AddAsync(NuevoFunko(_categoria.Id, "Zed"));
            await _repository.DeleteAsync(borrado.Id);

            // act
            var resultado = await _repository.GetAllAsync();

            // assert
            resultado.Select(f => f.Nombre).Should().Equal("Ant-Man", "Venom");
        }

        [TestCase(1, 10, 10, "Funko 001", TestName = "GetAllPagedAsync_PrimeraPagina_DevuelveLosPrimeros")]
        [TestCase(2, 10, 10, "Funko 011", TestName = "GetAllPagedAsync_SegundaPagina_DevuelveLosSiguientes")]
        [TestCase(11, 10, 1, "Funko 101", TestName = "GetAllPagedAsync_UltimaPagina_DevuelveElResto")]
        public async Task GetAllPagedAsync_PaginaValida_DevuelveLoEsperado(
            int page, int pageSize, int esperados, string primerNombre) {
            // arrange
            await SembrarAsync(_context, _categoria.Id, 101);

            // act
            var resultado = await _repository.GetAllPagedAsync(page, pageSize);

            // assert
            resultado.Should().HaveCount(esperados);
            resultado.First().Nombre.Should().Be(primerNombre);
        }

        [Test]
        public async Task GetByIdAsync_FunkoExiste_DevuelveFunko() {
            // arrange
            var creado = await _repository.AddAsync(NuevoFunko(_categoria.Id, "Iron Man"));

            // act
            var resultado = await _repository.GetByIdAsync(creado.Id);

            // assert
            resultado.Should().NotBeNull();
            resultado!.Nombre.Should().Be("Iron Man");
            resultado.CategoriaId.Should().Be(_categoria.Id);
        }

        [Test]
        public async Task UpdateAsync_FunkoExiste_ActualizaCamposYFecha() {
            // arrange
            var otraCategoria = await new CategoriaRepository(_context).AddAsync(new Categoria { Nombre = "DC" });
            var creado = await _repository.AddAsync(NuevoFunko(_categoria.Id, "Batman"));
            var cambios = new Funko {
                Id = creado.Id,
                Nombre = "Superman",
                Precio = 45.75m,
                Stock = 8,
                Imagen = "superman.png",
                CategoriaId = otraCategoria.Id,
                ProveedorId = "507f1f77bcf86cd799439022"
            };

            // act
            var resultado = await _repository.UpdateAsync(cambios);

            // assert
            resultado.Should().NotBeNull();
            resultado!.UpdatedAt.Should().NotBeNull();

            await using var otro = NewContext();
            var enBd = await otro.Funkos.FirstAsync(f => f.Id == creado.Id);
            enBd.Nombre.Should().Be("Superman");
            enBd.Precio.Should().Be(45.75m);
            enBd.Stock.Should().Be(8);
            enBd.Imagen.Should().Be("superman.png");
            enBd.CategoriaId.Should().Be(otraCategoria.Id);
            enBd.ProveedorId.Should().Be("507f1f77bcf86cd799439022");
        }

        [Test]
        public async Task DeleteAsync_FunkoExiste_BorradoLogico() {
            // arrange
            var creado = await _repository.AddAsync(NuevoFunko(_categoria.Id));

            // act
            var resultado = await _repository.DeleteAsync(creado.Id);

            // assert
            resultado.Should().BeTrue();

            await using var otro = NewContext();
            var enBd = await otro.Funkos.IgnoreQueryFilters().FirstAsync(f => f.Id == creado.Id);
            enBd.IsDeleted.Should().BeTrue();
            enBd.DeletedAt.Should().NotBeNull();
        }

        [Test]
        public async Task ExistsByCategoriaAsync_ConFunkoEnLaCategoria_DevuelveTrue() {
            // arrange
            await _repository.AddAsync(NuevoFunko(_categoria.Id));

            // act
            var resultado = await _repository.ExistsByCategoriaAsync(_categoria.Id);

            // assert
            resultado.Should().BeTrue();
        }

        [Test]
        public async Task ExistsByProveedorAsync_ConFunkoDelProveedor_DevuelveTrue() {
            // arrange
            await _repository.AddAsync(NuevoFunko(_categoria.Id));

            // act
            var resultado = await _repository.ExistsByProveedorAsync(ProveedorId);

            // assert
            resultado.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosInvalidos : PostgresTestBase {

        private AppDbContext _context = null!;
        private IFunkosRepository _repository = null!;
        private Categoria _categoria = null!;

        [SetUp]
        public async Task SetUp() {
            _context = NewContext();

            // BD limpia en cada test: primero funkos (FK Restrict) y después categorías
            await _context.Funkos.IgnoreQueryFilters().ExecuteDeleteAsync();
            await _context.Categorias.IgnoreQueryFilters().ExecuteDeleteAsync();

            _repository = new FunkosRepository(_context);

            _categoria = await new CategoriaRepository(_context).AddAsync(new Categoria { Nombre = "Marvel" });
        }

        [TearDown]
        public async Task TearDown() => await _context.DisposeAsync();

        // el repo corrige los parámetros fuera de rango en vez de fallar
        [TestCase(0, 10, 10, "Funko 001", TestName = "GetAllPagedAsync_PaginaMenorQueUno_UsaLaPrimera")]
        [TestCase(1, 0, 10, "Funko 001", TestName = "GetAllPagedAsync_TamanoMenorQueUno_UsaDiez")]
        [TestCase(1, 500, 100, "Funko 001", TestName = "GetAllPagedAsync_TamanoMayorQueCien_LimitaACien")]
        public async Task GetAllPagedAsync_ParametrosFueraDeRango_LosCorrige(
            int page, int pageSize, int esperados, string primerNombre) {
            // arrange
            await SembrarAsync(_context, _categoria.Id, 101);

            // act
            var resultado = await _repository.GetAllPagedAsync(page, pageSize);

            // assert
            resultado.Should().HaveCount(esperados);
            resultado.First().Nombre.Should().Be(primerNombre);
        }

        [TestCase(false, TestName = "GetByIdAsync_FunkoNoExiste_DevuelveNull")]
        [TestCase(true, TestName = "GetByIdAsync_FunkoBorrado_DevuelveNull")]
        public async Task GetByIdAsync_FunkoNoDisponible_DevuelveNull(bool borrado) {
            // arrange
            var id = await IdNoDisponibleAsync(_repository, _categoria.Id, borrado);

            // act
            var resultado = await _repository.GetByIdAsync(id);

            // assert
            resultado.Should().BeNull();
        }

        [TestCase(false, TestName = "UpdateAsync_FunkoNoExiste_DevuelveNull")]
        [TestCase(true, TestName = "UpdateAsync_FunkoBorrado_DevuelveNull")]
        public async Task UpdateAsync_FunkoNoDisponible_DevuelveNull(bool borrado) {
            // arrange
            var cambios = NuevoFunko(_categoria.Id, "Nuevo nombre");
            cambios.Id = await IdNoDisponibleAsync(_repository, _categoria.Id, borrado);

            // act
            var resultado = await _repository.UpdateAsync(cambios);

            // assert
            resultado.Should().BeNull();
        }

        [TestCase(false, TestName = "DeleteAsync_FunkoNoExiste_DevuelveFalse")]
        [TestCase(true, TestName = "DeleteAsync_FunkoYaBorrado_DevuelveFalse")]
        public async Task DeleteAsync_FunkoNoDisponible_DevuelveFalse(bool borrado) {
            // arrange
            var id = await IdNoDisponibleAsync(_repository, _categoria.Id, borrado);

            // act
            var resultado = await _repository.DeleteAsync(id);

            // assert
            resultado.Should().BeFalse();
        }

        [TestCase(false, TestName = "ExistsByCategoriaAsync_SinFunkos_DevuelveFalse")]
        [TestCase(true, TestName = "ExistsByCategoriaAsync_SoloFunkosBorrados_DevuelveFalse")]
        public async Task ExistsByCategoriaAsync_SinFunkosActivos_DevuelveFalse(bool conFunkoBorrado) {
            // arrange
            if (conFunkoBorrado) await IdNoDisponibleAsync(_repository, _categoria.Id, true);

            // act
            var resultado = await _repository.ExistsByCategoriaAsync(_categoria.Id);

            // assert
            resultado.Should().BeFalse();
        }

        [TestCase(false, TestName = "ExistsByProveedorAsync_SinFunkos_DevuelveFalse")]
        [TestCase(true, TestName = "ExistsByProveedorAsync_SoloFunkosBorrados_DevuelveFalse")]
        public async Task ExistsByProveedorAsync_SinFunkosActivos_DevuelveFalse(bool conFunkoBorrado) {
            // arrange
            if (conFunkoBorrado) await IdNoDisponibleAsync(_repository, _categoria.Id, true);

            // act
            var resultado = await _repository.ExistsByProveedorAsync(ProveedorId);

            // assert
            resultado.Should().BeFalse();
        }
    }
}