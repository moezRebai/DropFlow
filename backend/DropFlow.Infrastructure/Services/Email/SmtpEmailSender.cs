using System.Net;
using System.Net.Mail;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Emails;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Configurations;
using DropFlow.Domain.Emails;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DropFlow.Infrastructure.Services.Email;

public class SmtpEmailSender(
    IOptions<EmailSettings> emailSettings,
    ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task<bool> SendAsync(
        EmailRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequest(request);
            
            using var message = CreateMailMessage(request);
            using var smtp = CreateSmtpClient();
            
            logger.LogInformation(
                "Sending email to {To} with subject: {Subject}", 
                request.To, 
                request.Subject
            );
            
            await smtp.SendMailAsync(message, cancellationToken);
            
            logger.LogInformation(
                "Email sent successfully to {To}", 
                request.To
            );
            
            return true;
        }
        catch (SmtpException ex)
        {
            logger.LogError(
                ex, 
                "SMTP error while sending email to {To}: {Message}", 
                request.To, 
                ex.Message
            );
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, 
                "Unexpected error while sending email to {To}", 
                request.To
            );
            return false;
        }
    }

    public async Task<bool> SendWithTemplateAsync(
        string templateName, 
        EmailRequest request, 
        object model, 
        CancellationToken cancellationToken = default)
    {
        // TODO: Implémenter le système de templates (Phase 2)
        // Pour l'instant, on utilise SendAsync directement
        return await SendAsync(request, cancellationToken);
    }

    private MailMessage CreateMailMessage(EmailRequest request)
    {
        var message = new MailMessage
        {
            From = new MailAddress(
                _emailSettings.Smtp.FromEmail, 
                _emailSettings.Smtp.FromName
            ),
            Subject = request.Subject,
            Body = request.Body,
            IsBodyHtml = request.IsHtml,
            Priority = MailPriority.Normal
        };

        // Destinataire
        message.To.Add(new MailAddress(request.To, request.ToName));

        // Pièces jointes
        if (request.Attachments?.Any() == true)
        {
            foreach (var attachment in request.Attachments)
            {
                var stream = new MemoryStream(attachment.Content);
                message.Attachments.Add(new Attachment(
                    stream,
                    attachment.FileName,
                    attachment.ContentType
                ));
            }
        }

        // Headers personnalisés
        if (request.Headers?.Any() != true)
        {
            return message;
        }
        foreach (var header in request.Headers)
        {
            message.Headers.Add(header.Key, header.Value);
        }

        return message;
    }

    private SmtpClient CreateSmtpClient()
    {
        var smtp = new SmtpClient
        {
            Host = _emailSettings.Smtp.Host,
            Port = _emailSettings.Smtp.Port,
            EnableSsl = _emailSettings.Smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                _emailSettings.Smtp.Username,
                _emailSettings.Smtp.Password
            ),
            Timeout = _emailSettings.Smtp.Timeout
        };

        return smtp;
    }

    private static void ValidateRequest(EmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            throw new ArgumentException("Recipient email is required", nameof(request.To));

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ArgumentException("Email subject is required", nameof(request.Subject));

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ArgumentException("Email body is required", nameof(request.Body));

        // Validation format email
        try
        {
            var mailAddress = new MailAddress(request.To);
        }
        catch
        {
            throw new ArgumentException("Invalid email format", nameof(request.To));
        }
    }
}