using CSharpFunctionalExtensions;
using FunkoApi.Configuration;
using FunkoApi.Errors.Common;
using FunkoApi.Errors.Email;
using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace FunkoApi.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<Result<bool, DomainError>> SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = bodyHtml };

            using var client = new SmtpClient();

            var secureSocketOptions = _emailSettings.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_emailSettings.Server, _emailSettings.Port, secureSocketOptions);

            if (!string.IsNullOrEmpty(_emailSettings.UserName))
            {
                await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Correo enviado con éxito a {ToEmail} con asunto '{Subject}'", toEmail, subject);
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo a {ToEmail}", toEmail);
            return Result.Failure<bool, DomainError>(
                EmailErrors.SendFailed(ex.Message)
            );
        }
    }
}