using FluentAssertions;
using FunkoApi.Configuration;
using FunkoApi.Services.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FunkoApi.Tests.Services.Cache;

// patron AAA
[TestFixture]
[TestOf(typeof(MemoryCacheService))]
public class MemoryCacheServiceTest
{
    private MemoryCache _memoryCache = null!;
    private MemoryCacheService _service = null!;

    [SetUp]
    public void SetUp()
    {
        // instancia nueva por test => aislamiento total
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        var cacheOptions = Options.Create(new CacheOptions
        {
            DefaultExpirationMinutes = 5,
            SlidingExpirationMinutes = 1
        });

        _service = new MemoryCacheService(_memoryCache, cacheOptions);
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache.Dispose();
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
        await _service.SetAsync("k", "temporal", TimeSpan.FromMilliseconds(300));
        (await _service.GetAsync<string>("k")).Should().Be("temporal");

        await Task.Delay(600);

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
        var llamadas = 0;

        var resultados = await Task.WhenAll(
            _service.GetOrSetAsync("k", Factory),
            _service.GetOrSetAsync("k", Factory));

        llamadas.Should().Be(1);
        resultados.Should().OnlyContain(r => r == "valor");
        return;

        async Task<string> Factory()
        {
            Interlocked.Increment(ref llamadas);
            await Task.Delay(300);
            return "valor";
        }
    }
}