using FluentAssertions;
using FunkoApi.Configuration;
using FunkoApi.Services.Cache;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Testcontainers.Redis;

namespace FunkoApi.Tests.Services.Cache;

// patron AAA
[TestFixture]
[TestOf(typeof(RedisCacheService))]
public class RedisCacheServiceTest
{
    private RedisContainer _container = null!;
    private RedisCacheService _service = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _container.DisposeAsync();
    }

    [SetUp]
    public void SetUp()
    {
        // InstanceName unico por test (mismo contenedor) => claves aisladas
        var redisOptions = Options.Create(new RedisCacheOptions
        {
            Configuration = _container.GetConnectionString(),
            InstanceName = $"test:{Guid.NewGuid()}:"
        });

        var cacheOptions = Options.Create(new CacheOptions
        {
            DefaultExpirationMinutes = 5,
            SlidingExpirationMinutes = 1
        });

        _service = new RedisCacheService(new RedisCache(redisOptions), cacheOptions);
    }

    [Test]
    public async Task SetAsync_ConValorValido_DeberiaPoderRecuperarse()
    {
        await _service.SetAsync("k", "uno");

        (await _service.GetAsync<string>("k")).Should().Be("uno");
    }

    [Test]
    public async Task SetAsync_ConExpiracion_DeberiaEliminarseTrasExpirar()
    {
        await _service.SetAsync("k", "temporal", TimeSpan.FromSeconds(2));
        (await _service.GetAsync<string>("k")).Should().Be("temporal");

        await Task.Delay(TimeSpan.FromSeconds(3));

        (await _service.GetAsync<string>("k")).Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ClaveInexistente_DeberiaRetornarDefault()
    {
        (await _service.GetAsync<string>("no-existe")).Should().BeNull();
    }

    [Test]
    public async Task RemoveAsync_ClaveExistente_DeberiaEliminarla()
    {
        await _service.SetAsync("k", "uno");

        await _service.RemoveAsync("k");

        (await _service.GetAsync<string>("k")).Should().BeNull();
    }

    [Test]
    public async Task ExistsAsync_DeberiaDevolverTrueSiExisteYFalseSiNo()
    {
        await _service.SetAsync("k", "uno");

        (await _service.ExistsAsync("k")).Should().BeTrue();
        (await _service.ExistsAsync("no-existe")).Should().BeFalse();
    }

    [Test]
    public async Task GetOrSetAsync_ClaveEnCache_NoDeberiaLlamarALaFactory()
    {
        await _service.SetAsync("k", "cacheado");
        var llamadas = 0;

        var result = await _service.GetOrSetAsync("k", () =>
        {
            llamadas++;
            return Task.FromResult("nuevo");
        });

        result.Should().Be("cacheado");
        llamadas.Should().Be(0);
    }

    [Test]
    public async Task GetOrSetAsync_ClaveInexistente_DeberiaLlamarALaFactoryYGuardar()
    {
        var result = await _service.GetOrSetAsync("k", () => Task.FromResult("nuevo"));

        result.Should().Be("nuevo");
        (await _service.GetAsync<string>("k")).Should().Be("nuevo");
    }

    [Test]
    public async Task GetOrSetAsync_FactoryDevuelveNull_NoDeberiaGuardarNada()
    {
        var result = await _service.GetOrSetAsync<string>("k", () => Task.FromResult<string>(null!));

        result.Should().BeNull();
        (await _service.ExistsAsync("k")).Should().BeFalse();
    }

    [Test]
    public async Task GetOrSetAsync_LlamadasConcurrentes_DeberiaEjecutarLaFactorySoloUnaVez()
    {
        // La 2ª llamada pasa el primer check (miss), espera el lock y
        // encuentra el valor en el double-check dentro del lock
        var llamadas = 0;

        async Task<string> Factory()
        {
            Interlocked.Increment(ref llamadas);
            await Task.Delay(300);
            return "valor";
        }

        var resultados = await Task.WhenAll(
            _service.GetOrSetAsync("k", Factory),
            _service.GetOrSetAsync("k", Factory));

        llamadas.Should().Be(1);
        resultados.Should().OnlyContain(r => r == "valor");
    }
}