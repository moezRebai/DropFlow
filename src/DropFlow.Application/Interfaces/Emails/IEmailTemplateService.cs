namespace DropFlow.Application.Interfaces.Emails;

public interface IEmailTemplateService
{
    string GetWelcomeTemplate(string firstName, string url);
    string GetUserInvitationTemplate(string companyName, string inviteUrl);
    string GetPasswordResetTemplate(string userName, string resetUrl);
    string GetDeliveryNoteTemplate(string deliveryReference, string clientName, string address);
    string GetInvoiceTemplate(string invoiceNumber, decimal amount);
}