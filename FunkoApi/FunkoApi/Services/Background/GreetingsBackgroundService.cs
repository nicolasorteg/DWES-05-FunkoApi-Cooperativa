using FunkoApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FunkoApi.Services.Background;

/// <summary>
/// Tarea programada en segundo plano que envía un saludo cada 15 segundos a todos los clientes de SignalR.
/// </summary>
public class GreetingBackgroundService(
    IHubContext<FunkosHub> hubContext,
    ILogger<GreetingBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 Servicio en segundo plano iniciado. Intervalo: {Interval}s", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var mensaje = $"¡Hola! Notificación periódica del servidor a las {DateTime.Now:HH:mm:ss}";

                // Emite el evento 'ReceiveGreeting' a todos los clientes conectados al Hub
                await hubContext.Clients.All.SendAsync("ReceiveGreeting", mensaje, stoppingToken);

                logger.LogInformation("📢 [SignalR Background] Saludo enviado: {Mensaje}", mensaje);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("🛑 Servicio en segundo plano detenido.");
        }
    }
}