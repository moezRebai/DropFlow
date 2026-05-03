using DropFlow.Domain.Maps;

namespace DropFlow.Application.Interfaces;

public interface IGeocodingService
{
    Task<GeocodeAddress> GeocodeAddressAsync(
        string address, 
        string zipCode, 
        string city);

    Task<(GoogleDirectionsResponse Response, string ErrorMessage)> GetOptimizedRouteAsync(string originAddress, string wayPoints, bool optimize = true);
    
    Task<(GoogleDirectionsResponse Response, string ErrorMessage)> GetDirectionsAsync(
        string originAddress, 
        string destinationAddress);
}