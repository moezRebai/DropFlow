namespace DropFlow.Shared.Drivers;

public class DriverDashboardResponse
{
    public DriverTodayResponse TodayRoute { get; set; } = new();
    public List<DriverRouteSummaryDto> UpcomingRoutes { get; set; } = [];
}
