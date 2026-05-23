namespace DropFlow.Application.Common;

public static class CacheKeys
{
    public static string DashboardStats(int t)              => $"df:{t}:dashboard:stats";
    public static string DashboardTodayDeliveries(int t)   => $"df:{t}:dashboard:today";
    public static string DashboardRisky(int t)             => $"df:{t}:dashboard:risky";
    public static string DashboardRevenue(int t, string p) => $"df:{t}:dashboard:revenue:{p}";
    public static string DashboardStatus(int t, string p)  => $"df:{t}:dashboard:status:{p}";
    public static string DashboardStoreChart(int t, string p) => $"df:{t}:dashboard:store-chart:{p}";
    public static string TimeSlots(int t)                  => $"df:{t}:timeslots";
    public static string StoresLookup(int t)               => $"df:{t}:stores:lookup";
    public static string ActiveDrivers(int t)              => $"df:{t}:drivers:active";
    public static string AvailableVehicles(int t, DateTime d) => $"df:{t}:vehicles:available:{d:yyyyMMdd}";
    public static string Depots(int t)                     => $"df:{t}:depots";
}
