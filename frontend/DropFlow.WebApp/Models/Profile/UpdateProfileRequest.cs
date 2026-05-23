namespace DropFlow.WebApp.Models.Profile;

/// <summary>
/// Request pour PUT /api/profile
/// </summary>
public class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}