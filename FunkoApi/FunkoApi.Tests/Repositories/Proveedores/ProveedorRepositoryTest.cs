using FluentAssertions;
using FunkoApi.Entity;
using FunkoApi.Models;
using FunkoApi.Repositories.Proveedores;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using NUnit.Framework;

namespace FunkoApi.Tests;

[TestFixture]
public class ProveedorRepositoryTests : MongoTestBase {
    private AppMongoDbContext _context = null!;
    private ProveedorRepository _repository = null!;

    private AppMongoDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppMongoDbContext>()
            .UseMongoDB(Client, DbName)
            .Options);

    [SetUp]
    public void SetUp() {
        Client.DropDatabase(DbName);

        _context = NewContext();
        _repository = new ProveedorRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static Proveedor NuevoProveedor(string nombre = "Logitech") => new() {
        Nombre = nombre,
        Email = $"{nombre.ToLower()}@test.com",
        Telefono = "600000000"
    };

    [Test]
    public async Task AddAsync_ProveedorValido_GuardaConIdYFlags() {
        var resultado = await _repository.AddAsync(NuevoProveedor());

        resultado.Id.Should().NotBe(ObjectId.Empty);
        resultado.IsDeleted.Should().BeFalse();
        resultado.UpdatedAt.Should().NotBeNull();

        await using var otro = NewContext();
        (await otro.Proveedores.AnyAsync(p => p.Id == resultado.Id)).Should().BeTrue();
    }

    [Test]
    public async Task GetAllAsync_SinDatos_DevuelveListaVacia() {
        var resultado = await _repository.GetAllAsync();

        resultado.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_ConDatos_DevuelveTodos() {
        await _repository.AddAsync(NuevoProveedor("Logitech"));
        await _repository.AddAsync(NuevoProveedor("Ikea"));

        var resultado = await _repository.GetAllAsync();

        resultado.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAllPagedAsync_PrimeraPagina_DevuelveOrdenadosPorNombre() {
        await _repository.AddAsync(NuevoProveedor("Cesar"));
        await _repository.AddAsync(NuevoProveedor("Ana"));
        await _repository.AddAsync(NuevoProveedor("Bruno"));

        var resultado = await _repository.GetAllPagedAsync(1, 2);

        resultado.Select(p => p.Nombre).Should().Equal("Ana", "Bruno");
    }

    [Test]
    public async Task GetAllPagedAsync_SegundaPagina_DevuelveElResto() {
        await _repository.AddAsync(NuevoProveedor("Cesar"));
        await _repository.AddAsync(NuevoProveedor("Ana"));
        await _repository.AddAsync(NuevoProveedor("Bruno"));

        var resultado = await _repository.GetAllPagedAsync(2, 2);

        resultado.Select(p => p.Nombre).Should().Equal("Cesar");
    }

    [Test]
    public async Task GetAllPagedAsync_ValoresPorDefecto_Funciona() {
        await _repository.AddAsync(NuevoProveedor());

        var resultado = await _repository.GetAllPagedAsync();

        resultado.Should().HaveCount(1);
    }

    [Test]
    public async Task GetByIdAsync_IdNoValido_DevuelveNull() {
        var resultado = await _repository.GetByIdAsync("esto-no-es-un-objectid");

        resultado.Should().BeNull();
    }

    [Test]
    public async Task GetByIdAsync_IdValidoQueNoExiste_DevuelveNull() {
        var resultado = await _repository.GetByIdAsync(ObjectId.GenerateNewId().ToString());

        resultado.Should().BeNull();
    }

    [Test]
    public async Task GetByIdAsync_ProveedorExiste_DevuelveProveedor() {
        var creado = await _repository.AddAsync(NuevoProveedor("Logitech"));

        var resultado = await _repository.GetByIdAsync(creado.Id.ToString());

        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Be("Logitech");
    }

    [Test]
    public async Task UpdateAsync_ProveedorNoExiste_DevuelveNull() {
        var inexistente = NuevoProveedor();
        inexistente.Id = ObjectId.GenerateNewId();

        var resultado = await _repository.UpdateAsync(inexistente);

        resultado.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_ProveedorExiste_ActualizaCampos() {
        var creado = await _repository.AddAsync(NuevoProveedor("Logitech"));

        var cambios = new Proveedor {
            Id = creado.Id,
            Nombre = "Logitech Pro",
            Email = "nuevo@test.com",
            Telefono = "611111111"
        };

        var resultado = await _repository.UpdateAsync(cambios);

        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Be("Logitech Pro");
        resultado.Email.Should().Be("nuevo@test.com");
        resultado.Telefono.Should().Be("611111111");

        await using var otro = NewContext();
        var enBd = await otro.Proveedores.FirstAsync(p => p.Id == creado.Id);
        enBd.Nombre.Should().Be("Logitech Pro");
    }

    [Test]
    public async Task DeleteAsync_IdNoValido_DevuelveFalse() {
        var resultado = await _repository.DeleteAsync("no-valido");

        resultado.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_ProveedorNoExiste_DevuelveFalse() {
        var resultado = await _repository.DeleteAsync(ObjectId.GenerateNewId().ToString());

        resultado.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_ProveedorExiste_BorradoLogico() {
        var creado = await _repository.AddAsync(NuevoProveedor());

        var resultado = await _repository.DeleteAsync(creado.Id.ToString());

        resultado.Should().BeTrue();

        await using var otro = NewContext();
        var enBd = await otro.Proveedores.FirstAsync(p => p.Id == creado.Id);
        enBd.IsDeleted.Should().BeTrue();
        enBd.DeletedAt.Should().NotBeNull();
    }
}