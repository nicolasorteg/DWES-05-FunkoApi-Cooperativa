using FunkoApi.Entity;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FunkoApi.Tests.Repositories;

public abstract class PostgresTestBase {
    
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    // nuevo context para creado
    protected AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options);

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        await _postgres.StartAsync();

        // crea tablas 1 vez
        await using var context = NewContext();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await _postgres.DisposeAsync();
    }
}