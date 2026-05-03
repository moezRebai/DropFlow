using DropFlow.Application.Interfaces.Emails;
using DropFlow.Domain.Emails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Emails;

public class EmailService(
    IConfiguration configuration,
    IEmailSender emailSender,
    IEmailTemplateService templateService,
    ILogger<EmailService> logger)
    : IEmailService
{
    public async Task SendInvitationEmailAsync(string email, string token, string companyName)
    {
        try
        {
            logger.LogInformation("Sending invitation email to {Email} for company {CompanyName}", email, companyName);

            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(email);
            
            var inviteUrl = $"{configuration["AppUrl"]}/accept-invitation?token={encodedToken}&&email={encodedEmail}";
            var subject = $"Invitation à rejoindre {companyName} sur DropFlow";
            
            // ✅ Utiliser le template professionnel
            var body = templateService.GetUserInvitationTemplate(companyName, inviteUrl);

            await SendEmailAsync(email, subject, body);
            
            logger.LogInformation("Invitation email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invitation email to {Email}", email);
            throw; // Propager l'exception pour que l'appelant puisse la gérer
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string token, string? userName,
        string tenantName,
        int tenantId,
        string role)
    {
        try
        {
            logger.LogInformation("Sending password reset email to {Email}", email);

            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(email);
    
            // ✅ URL avec TenantId
            var resetUrl = $"{configuration["AppUrl"]}/reset-password" +
                           $"?email={encodedEmail}" +
                           $"&token={encodedToken}" +
                           $"&tenantId={tenantId}";  // ✅ AJOUT

            logger.LogDebug("Password reset URL generated: {Url}", resetUrl);

            var name = string.IsNullOrEmpty(userName) ? email : userName;
            const string subject = "Réinitialisation de votre mot de passe DropFlow";
            
            // ✅ Utiliser le template professionnel
            var body = templateService.GetPasswordResetTemplate(name, resetUrl);

            await SendEmailAsync(email, subject, body);
            
            logger.LogInformation("Password reset email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            throw;
        }
    }
    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        try
        {
            logger.LogInformation("Sending welcome email to {Email}", email);

            var subject = $"Bienvenue sur DropFlow, {firstName} !";

            var url = configuration["AppUrl"];
            
            var body = templateService.GetWelcomeTemplate(firstName, url);

            await SendEmailAsync(email, subject, body);
            
            logger.LogInformation("Welcome email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            throw;
        }
    }
    private async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var request = new EmailRequest
            {
                To = to,
                Subject = subject,
                Body = body,
                IsHtml = true
            };

            logger.LogDebug("Sending email to {To} with subject: {Subject}", to, subject);

            var success = await emailSender.SendAsync(request);

            if (!success)
            {
                logger.LogWarning("Email sending returned false for {To}", to);
                throw new InvalidOperationException($"Failed to send email to {to}");
            }

            logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {To}", to);
            throw; // Propager pour que l'appelant puisse gérer
        }
    }
}