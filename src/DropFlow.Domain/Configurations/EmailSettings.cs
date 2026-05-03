namespace DropFlow.Domain.Configurations;

public class EmailSettings
{
    public string Provider { get; set; } = "Smtp"; // Smtp, SendGrid, Mailgun
    public SmtpSettings Smtp { get; set; } = new();
    public Dictionary<string, string> DefaultSubjects { get; set; } = new();
}