using FluentAssertions;
using FunkoApi.Entity;
using FunkoApi.Models;
using FunkoApi.Repositories.Categorias;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Tests.Repositories.Categorias;

[TestFixture]
public class CategoriaRepositoryTests : PostgresTestBase {
    private AppDbContext _context = null!;
    private CategoriaRepository _repository = null!;

    [SetUp]
    public async Task SetUp() {
        _context = NewContext();

        // BD limpia en cada test (IgnoreQueryFilters por si hay filtro de IsDeleted)
        await _context.Categorias.IgnoreQueryFilters().ExecuteDeleteAsync();

        _repository = new CategoriaRepository(_context);
    }

    [TearDown]
    public async Task TearDown() => await _context.DisposeAsync();

    private static Categoria NuevaCategoria(string nombre = "Cómics") => new() { Nombre = nombre };

    // ---------- AddAsync ----------

    [Test]
    public async Task AddAsync_CategoriaValida_GeneraIdYRellenaCampos() {
        var entrada = NuevaCategoria();
        entrada.IsDeleted = true; // el repo debe forzarlo a false

        var resultado = await _repository.AddAsync(entrada);

        resultado.Id.Should().NotBeNullOrEmpty();
        resultado.IsDeleted.Should().BeFalse();
        resultado.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Se comprueba con otro contexto: viene de la BD, no del tracker
        await using var otro = NewContext();
        (await otro.Categorias.AnyAsync(c => c.Id == resultado.Id)).Should().BeTrue();
    }

    // ---------- GetAllAsync ----------

    [Test]
    public async Task GetAllAsync_SinDatos_DevuelveListaVacia() {
        var resultado = await _repository.GetAllAsync();

        resultado.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_ConDatos_DevuelveTodas() {
        await _repository.AddAsync(NuevaCategoria("Cómics"));
        await _repository.AddAsync(NuevaCategoria("Películas"));

        var resultado = await _repository.GetAllAsync();

        resultado.Should().HaveCount(2);
    }

    // ---------- GetAllPagedAsync ----------

    [Test]
    public async Task GetAllPagedAsync_PrimeraPagina_DevuelveOrdenadasPorNombre() {
        await _repository.AddAsync(NuevaCategoria("Cesar"));
        await _repository.AddAsync(NuevaCategoria("Ana"));
        await _repository.AddAsync(NuevaCategoria("Bruno"));

        var resultado = await _repository.GetAllPagedAsync(1, 2);

        resultado.Select(c => c.Nombre).Should().Equal("Ana", "Bruno");
    }

    [Test]
    public async Task GetAllPagedAsync_SegundaPagina_DevuelveElResto() {
        await _repository.AddAsync(NuevaCategoria("Cesar"));
        await _repository.AddAsync(NuevaCategoria("Ana"));
        await _repository.AddAsync(NuevaCategoria("Bruno"));

        var resultado = await _repository.GetAllPagedAsync(2, 2);

        resultado.Select(c => c.Nombre).Should().Equal("Cesar");
    }

    [Test]
    public async Task GetAllPagedAsync_ValoresPorDefecto_Funciona() {
        await _repository.AddAsync(NuevaCategoria());

        var resultado = await _repository.GetAllPagedAsync();

        resultado.Should().HaveCount(1);
    }

    // ---------- GetByIdAsync ----------

    [Test]
    public async Task GetByIdAsync_CategoriaExiste_DevuelveCategoria() {
        var creada = await _repository.AddAsync(NuevaCategoria("Cómics"));

        var resultado = await _repository.GetByIdAsync(creada.Id);

        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Be("Cómics");
    }

    [Test]
    public async Task GetByIdAsync_CategoriaNoExiste_DevuelveNull() {
        var resultado = await _repository.GetByIdAsync("no-existe");

        resultado.Should().BeNull();
    }

    // ---------- UpdateAsync ----------

    [Test]
    public async Task UpdateAsync_CategoriaNoExiste_DevuelveNull() {
        var inexistente = NuevaCategoria();
        inexistente.Id = "no-existe";

        var resultado = await _repository.UpdateAsync(inexistente);

        resultado.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_CategoriaExiste_ActualizaNombreYFecha() {
        var creada = await _repository.AddAsync(NuevaCategoria("Cómics"));

        var resultado = await _repository.UpdateAsync(new Categoria { Id = creada.Id, Nombre = "Superhéroes" });

        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Be("Superhéroes");
        resultado.UpdatedAt.Should().NotBeNull();

        await using var otro = NewContext();
        var enBd = await otro.Categorias.FirstAsync(c => c.Id == creada.Id);
        enBd.Nombre.Should().Be("Superhéroes");
    }

    // ---------- DeleteAsync ----------

    [Test]
    public async Task DeleteAsync_CategoriaNoExiste_DevuelveFalse() {
        var resultado = await _repository.DeleteAsync("no-existe");

        resultado.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_CategoriaExiste_BorradoLogico() {
        var creada = await _repository.AddAsync(NuevaCategoria());

        var resultado = await _repository.DeleteAsync(creada.Id);

        resultado.Should().BeTrue();

        await using var otro = NewContext();
        var enBd = await otro.Categorias.IgnoreQueryFilters().FirstAsync(c => c.Id == creada.Id);
        enBd.IsDeleted.Should().BeTrue();
        enBd.DeletedAt.Should().NotBeNull();
    }
}