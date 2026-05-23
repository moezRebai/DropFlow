using System.ComponentModel.DataAnnotations;

namespace DropFlow.WebApp.Models.Deliveries;

public class DeliveryItemModel
{
    public string? Reference { get; set; }
        
    [Required(ErrorMessage = "La désignation est obligatoire")]
    public string Designation { get; set; } = string.Empty;
        
    [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être ≥ 1")]
    public int Quantity { get; set; } = 1;
    public string? Information { get; set; }
}