using DropFlow.Domain.Common;

namespace DropFlow.Domain.Entities;

public class Vehicle : ITenantEntity
{
    public int Id { get; private set; }
    public int TenantId { get; set; }
    
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public string PlateNumber { get; private set; }
    
    public int MaxDeliveries { get; private set; }
    public int MaxVolume { get; private set; }
    
    public bool IsActive { get; private set; }
    public DateTime CreatedDate { get; private set; }
    
    // Navigation
    public List<Route> Routes { get; set; } = new();
    
    private Vehicle()
    {
        Brand = string.Empty;
        Model = string.Empty;
        PlateNumber = string.Empty;
    }
    
    public static Vehicle Create(
        string brand,
        string model,
        string plateNumber,
        int maxDeliveries,
        int maxVolume)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Brand is required", nameof(brand));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model is required", nameof(model));
        if (string.IsNullOrWhiteSpace(plateNumber))
            throw new ArgumentException("Plate number is required", nameof(plateNumber));
        if (maxDeliveries <= 0)
            throw new ArgumentException("Max deliveries must be positive", nameof(maxDeliveries));
        if (maxVolume <= 0)
            throw new ArgumentException("Max volume must be positive", nameof(maxVolume));
        
        return new Vehicle
        {
            Brand = brand,
            Model = model,
            PlateNumber = plateNumber,
            MaxDeliveries = maxDeliveries,
            MaxVolume = maxVolume,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
    }
    
    public void Update(
        string brand,
        string model,
        string plateNumber,
        int maxDeliveries,
        int maxVolume,
        bool isActive)
    {
        Brand = brand;
        Model = model;
        PlateNumber = plateNumber;
        MaxDeliveries = maxDeliveries;
        MaxVolume = maxVolume;
        IsActive = isActive;
    }
    
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}