namespace DropFlow.Domain.Entities;

public class UserPreferences
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public virtual ApplicationUser User { get; private set; } = null!;
    
    // Notifications
    public bool EmailNotificationsEnabled { get; private set; } = true;
    public bool SmsNotificationsEnabled { get; private set; } = false;
    public bool PushNotificationsEnabled { get; private set; } = true;
    
    // Notifications spécifiques
    public bool DeliveryStatusNotifications { get; private set; } = true;
    public bool InvitationNotifications { get; private set; } = true;
    public bool SystemNotifications { get; private set; } = true;
    
    // Préférences UI
    public string Theme { get; private set; } = "Light"; // Light, Dark, Auto
    public string Language { get; private set; } = "fr-FR";
    public string TimeZone { get; private set; } = "Europe/Paris";
    
    // Dates
    public DateTime CreatedDate { get; private set; }
    public DateTime? ModifiedDate { get; private set; }

    private UserPreferences() { }

    public static UserPreferences CreateDefault(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required", nameof(userId));

        return new UserPreferences
        {
            UserId = userId,
            EmailNotificationsEnabled = true,
            SmsNotificationsEnabled = false,
            PushNotificationsEnabled = true,
            DeliveryStatusNotifications = true,
            InvitationNotifications = true,
            SystemNotifications = true,
            Theme = "Light",
            Language = "fr-FR",
            TimeZone = "Europe/Paris",
            CreatedDate = DateTime.UtcNow
        };
    }

    public void UpdateNotificationSettings(
        bool emailEnabled,
        bool smsEnabled,
        bool pushEnabled,
        bool deliveryStatus,
        bool invitations,
        bool system)
    {
        EmailNotificationsEnabled = emailEnabled;
        SmsNotificationsEnabled = smsEnabled;
        PushNotificationsEnabled = pushEnabled;
        DeliveryStatusNotifications = deliveryStatus;
        InvitationNotifications = invitations;
        SystemNotifications = system;
        ModifiedDate = DateTime.UtcNow;
    }

    public void UpdateUiSettings(string theme, string language, string timeZone)
    {
        Theme = theme;
        Language = language;
        TimeZone = timeZone;
        ModifiedDate = DateTime.UtcNow;
    }
}