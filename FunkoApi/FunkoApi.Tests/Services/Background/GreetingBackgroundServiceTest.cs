using FluentAssertions;
using FunkoApi.Hubs;
using FunkoApi.Services.Background;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace FunkoApi.Tests.Services.Background;

[TestFixture]
public class GreetingBackgroundServiceTest
{
    private Mock<IHubContext<FunkosHub>> _hubContextMock = null!;
    private Mock<IHubClients> _hubClientsMock = null!;
    private Mock<IClientProxy> _clientProxyMock = null!;
    private Mock<ILogger<GreetingBackgroundService>> _loggerMock = null!;

    private GreetingBackgroundService _service = null!;

    [SetUp]
    public void Setup()
    {
        _hubContextMock = new Mock<IHubContext<FunkosHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _loggerMock = new Mock<ILogger<GreetingBackgroundService>>();

        _hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);

        _service = new GreetingBackgroundService(_hubContextMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task StartAsyncYStopAsync_DetieneElServicioLimpiaYRapidamente_CuandoSeCancelaElToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        await _service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        var stopAction = async () => await _service.StopAsync(CancellationToken.None);

        // Assert
        await stopAction.Should().NotThrowAsync();
        _clientProxyMock.Verify(
            p => p.SendCoreAsync(
                "ReceiveGreeting",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_EnviaSaludoATodosLosClientesSignalR_TrasPrimerTick()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        await _service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(15.3));
        await cts.CancelAsync();
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _clientProxyMock.Verify(
            p => p.SendCoreAsync(
                "ReceiveGreeting",
                It.Is<object?[]>(args =>
                    args.Length == 1 &&
                    args[0] != null &&
                    args[0]!.ToString()!.StartsWith("¡Hola! Notificación periódica del servidor a las ")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}