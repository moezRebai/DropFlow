namespace DropFlow.Mobile.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TenantId { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? Message { get; set; }
    public UserInfo? User { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
}

public class DashboardResponse
{
    public TodayRouteResponse TodayRoute { get; set; } = new();
    public List<RouteSummaryItem> UpcomingRoutes { get; set; } = [];
}

public class TodayRouteResponse
{
    public bool HasRoute { get; set; }
    public string? Message { get; set; }
    public RouteDto? Route { get; set; }
}

public class RouteDto
{
    public int RouteId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string DepartureAddress { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EstimatedEndTime { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public double TotalDistanceKm { get; set; }
    public int TotalDurationMinutes { get; set; }
    public List<string> TeamMembers { get; set; } = [];
    public List<DeliveryListItem> Deliveries { get; set; } = [];

    public int DeliveredCount => Deliveries.Count(d => d.Status == 3);
    public double ProgressPercent => TotalDeliveries > 0 ? (double)DeliveredCount / TotalDeliveries : 0;
}

public class DeliveryListItem
{
    public int Id { get; set; }
    public int SequenceOrder { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string TimeSlotName { get; set; } = string.Empty;
    public string EstimatedArrivalTime { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public bool WithAssembly { get; set; }
    public int TotalPackages { get; set; }
    public bool HasClientPayment { get; set; }
    public bool IsClientAbsent { get; set; }
    public bool IsValidated { get; set; }
}

public class DeliveryDetailDto
{
    public int Id { get; set; }
    public int SequenceOrder { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ClientFirstName { get; set; } = string.Empty;
    public string ClientLastName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? AddressComplement { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string FileNumber { get; set; } = string.Empty;
    public string ScheduledDate { get; set; } = string.Empty;
    public string TimeSlotName { get; set; } = string.Empty;
    public string EstimatedArrivalTime { get; set; } = string.Empty;
    public bool WithAssembly { get; set; }
    public int TotalPackages { get; set; }
    public decimal ClientPaymentAmount { get; set; }
    public string? DeliveryNotes { get; set; }
    public List<DeliveryItemDto> Items { get; set; } = [];
    public int Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public bool IsValidated { get; set; }
    public bool IsClientAbsent { get; set; }
    public bool HasSignature { get; set; }
    public bool HasPhoto { get; set; }

    public bool CanValidate => Status == 2 && !IsValidated;
    public bool HasPayment => ClientPaymentAmount > 0;
}

public class DeliveryItemDto
{
    public string Reference { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Information { get; set; }
}

public class ValidationRequest
{
    public string? SignatureBase64 { get; set; }
    public string? PhotoBase64 { get; set; }
    public string? Comment { get; set; }
    public bool IsClientAbsent { get; set; }
}

public class ApiMessageResponse
{
    public string Message { get; set; } = string.Empty;
}

public class DeliveryHistoryItem
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public bool IsClientAbsent { get; set; }
    public DateTime? DeliveredDateTime { get; set; }
    public string RouteReference { get; set; } = string.Empty;

    public string DateDisplay => Date.HasValue ? Date.Value.ToString("dd/MM/yyyy") : string.Empty;
}

public class DeliveryHistoryResponse
{
    public List<DeliveryHistoryItem> Deliveries { get; set; } = [];
    public int TotalCount { get; set; }
}

public class RouteSummaryItem
{
    public int RouteId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public int DeliveredCount { get; set; }
    public TimeSpan? StartTime { get; set; }
    public double TotalDistanceKm { get; set; }

    public string DateDisplay => Date.ToString("ddd dd/MM", new System.Globalization.CultureInfo("fr-FR"));
    public bool IsToday => Date.Date == DateTime.Today;
    public string StartTimeDisplay => StartTime.HasValue ? StartTime.Value.ToString(@"hh\:mm") : "--:--";
}
