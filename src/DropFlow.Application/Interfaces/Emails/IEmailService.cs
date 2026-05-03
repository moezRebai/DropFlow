namespace DropFlow.Application.Interfaces.Emails;

public interface IEmailService
{
    Task SendInvitationEmailAsync(string email, string token, string companyName);
    Task SendWelcomeEmailAsync(string email, string firstName);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string? userName,
        string tenantName,
        int tenantId,  
        string role);
}