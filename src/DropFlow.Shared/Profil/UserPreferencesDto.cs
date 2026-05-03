namespace DropFlow.Shared.Profil;

public class UserPreferencesDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    // Notifications Email
    public bool EmailNotificationsEnabled { get; set; }
    public bool EmailOnNewDelivery { get; set; }
    public bool EmailOnDeliveryCompleted { get; set; }
    public bool EmailOnInvoiceCreated { get; set; }
    public bool EmailOnInvoicePaid { get; set; }
    
    // Notifications SMS
    public bool SmsNotificationsEnabled { get; set; }
    public bool SmsOnUrgentDelivery { get; set; }
    public bool SmsOnDeliveryLate { get; set; }
    
    // Notifications In-App
    public bool InAppNotificationsEnabled { get; set; }
    
    // Préférences Interface
    public bool DarkModeEnabled { get; set; }
    public string Language { get; set; } = "fr";
    public string TimeZone { get; set; } = "Europe/Paris";
    
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
