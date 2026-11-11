using CSharpFunctionalExtensions;
using FunkoApi.Errors.Common;

namespace FunkoApi.Services.Email;

public interface IEmailService
{
    Task<Result<bool, DomainError>> SendEmailAsync(string toEmail, string subject, string bodyHtml);
}