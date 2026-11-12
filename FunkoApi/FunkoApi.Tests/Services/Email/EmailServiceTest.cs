using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using FunkoApi.Configuration;
using FunkoApi.Errors.Email;
using FunkoApi.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FunkoApi.Tests.Services.Email;

[TestFixture]
public class EmailServiceTest
{
    private Mock<ILogger<EmailService>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<EmailService>>();
    }

    private static EmailSettings CrearSettings(int port, string server = "127.0.0.1", string userName = "", bool enableSsl = false) =>
        new()
        {
            Server = server,
            Port = port,
            SenderName = "FunkoApi Store",
            SenderEmail = "noreply@funkoapi.es",
            UserName = userName,
            Password = "secret",
            EnableSsl = enableSsl
        };

    /// <summary>
    /// Levanta un servidor SMTP mínimo en localhost para probar el flujo completo de MailKit
    /// (ConnectAsync, EHLO, MAIL FROM, RCPT TO, DATA, QUIT) sin dependencias externas.
    /// </summary>
    private static (TcpListener Listener, int Port, Task ServerTask, List<string> ReceivedLines) IniciarServidorSmtpLocal()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var receivedLines = new List<string>();

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };

            await writer.WriteLineAsync("220 localhost ESMTP TestServer");

            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line is null) break;
                receivedLines.Add(line);

                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250-localhost");
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("354 Start mail input; end with <CRLF>.<CRLF>");
                    string? dataLine;
                    while ((dataLine = await reader.ReadLineAsync()) != null && dataLine != ".")
                    {
                        receivedLines.Add(dataLine);
                    }
                    await writer.WriteLineAsync("250 OK: queued");
                }
                else if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 Bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }
        });

        return (listener, port, serverTask, receivedLines);
    }

    [Test]
    public async Task SendEmailAsync_DevuelveSuccess_CuandoServidorSmtpAceptaElMensaje()
    {
        var (listener, port, serverTask, receivedLines) = IniciarServidorSmtpLocal();
        try
        {
            var settings = Options.Create(CrearSettings(port));
            var service = new EmailService(settings, _loggerMock.Object);

            var result = await service.SendEmailAsync(
                "cliente@funkoapi.es",
                "Bienvenido a FunkoApi",
                "<h1>¡Hola!</h1><p>Tu cuenta está lista.</p>");

            await serverTask;

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
            receivedLines.Should().Contain(l => l.Contains("MAIL FROM:<noreply@funkoapi.es>"));
            receivedLines.Should().Contain(l => l.Contains("RCPT TO:<cliente@funkoapi.es>"));
            receivedLines.Should().Contain(l => l.Contains("Subject: Bienvenido a FunkoApi"));
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task SendEmailAsync_DevuelveFailureSendFailed_CuandoEmailDestinoEsInvalido()
    {
        var settings = Options.Create(CrearSettings(2525));
        var service = new EmailService(settings, _loggerMock.Object);

        var result = await service.SendEmailAsync("email-no-valido", "Asunto", "<p>Cuerpo</p>");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EmailError.SendFailed>();
    }

    [Test]
    public async Task SendEmailAsync_DevuelveFailureSendFailed_CuandoEmailRemitenteEsInvalido()
    {
        var settings = Options.Create(new EmailSettings
        {
            Server = "127.0.0.1",
            Port = 2525,
            SenderName = "FunkoApi",
            SenderEmail = "remitente-invalido",
            EnableSsl = false
        });
        var service = new EmailService(settings, _loggerMock.Object);

        var result = await service.SendEmailAsync("cliente@funkoapi.es", "Asunto", "<p>Cuerpo</p>");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EmailError.SendFailed>();
    }

    [Test]
    public async Task SendEmailAsync_DevuelveFailureSendFailed_CuandoNoPuedeConectarConElServidor()
    {
        // Obtenemos un puerto libre y cerramos el listener para garantizar conexión rechazada
        var tempListener = new TcpListener(IPAddress.Loopback, 0);
        tempListener.Start();
        var unusedPort = ((IPEndPoint)tempListener.LocalEndpoint).Port;
        tempListener.Stop();

        var settings = Options.Create(CrearSettings(unusedPort));
        var service = new EmailService(settings, _loggerMock.Object);

        var result = await service.SendEmailAsync("cliente@funkoapi.es", "Asunto", "<p>Cuerpo</p>");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EmailError.SendFailed>();
        result.Error.Message.Should().Contain("Error al enviar el correo electrónico:");
    }

    [Test]
    public async Task SendEmailAsync_DevuelveFailureSendFailed_CuandoAutenticacionFallaPorServidorSinAuth()
    {
        var (listener, port, serverTask, _) = IniciarServidorSmtpLocal();
        try
        {
            // Con UserName relleno intentará AuthenticateAsync contra un servidor que no anuncia AUTH
            var settings = Options.Create(CrearSettings(port, userName: "usuario@funkoapi.es", enableSsl: false));
            var service = new EmailService(settings, _loggerMock.Object);

            var result = await service.SendEmailAsync("cliente@funkoapi.es", "Asunto", "<p>Cuerpo</p>");

            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<EmailError.SendFailed>();
        }
        finally
        {
            listener.Stop();
            await Task.WhenAny(serverTask, Task.Delay(500));
        }
    }
}