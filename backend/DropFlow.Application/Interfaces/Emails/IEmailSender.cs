using DropFlow.Domain.Emails;

namespace DropFlow.Application.Interfaces.Emails;

public interface IEmailSender
{
    Task<bool> SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
    Task<bool> SendWithTemplateAsync(string templateName, EmailRequest request, object model, CancellationToken cancellationToken = default);
}