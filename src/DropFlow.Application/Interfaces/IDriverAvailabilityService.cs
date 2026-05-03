using DropFlow.Shared.Drivers;

namespace DropFlow.Application.Interfaces;

public interface IDriverAvailabilityService
{
    Task<DriverAvailabilityDto> CheckAvailabilityAsync(int driverId, DateTime date);
    Task<List<DriverAvailabilityDto>> CheckMultipleAvailabilityAsync(List<int> driverIds, DateTime date);
    Task<List<DriverDto>> GetAvailableDriversAsync(DateTime date);
}