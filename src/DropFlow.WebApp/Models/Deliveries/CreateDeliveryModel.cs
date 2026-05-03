using System.ComponentModel.DataAnnotations;
using DropFlow.Domain.Enums;

namespace DropFlow.WebApp.Models.Deliveries;

public class CreateDeliveryModel
{
    // Client existant
    public int? ClientId { get; set; }
    public int? ClientAddressId { get; set; }
        
    // Nouveau client
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    public string ClientFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire")]
    public string ClientLastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email invalide")]
    public string? ClientEmail { get; set; }

    [Required(ErrorMessage = "Le téléphone est obligatoire")]
    public string ClientPhone { get; set; } = string.Empty;

    // Adresse
    [Required(ErrorMessage = "L'adresse est obligatoire")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le code postal est obligatoire")]
    public string ZipCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ville est obligatoire")]
    public string City { get; set; } = string.Empty;

    public string? AddressComplement { get; set; }

    // Détails
    [Required(ErrorMessage = "L'enseigne est obligatoire")]
    public int? StoreId { get; set; }

    [Required(ErrorMessage = "N° Dossier est obligatoire")]
    public string? FileNumber { get; set; }
    public DateTime? ScheduledDate { get; set; }

    [Required(ErrorMessage = "Le prix est obligatoire")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Le prix doit être supérieur à 0")]
    public decimal Price { get; set; }

    public decimal? ClientPaymentAmount { get; set; }
    public decimal? StorePaymentAmount { get; set; }

    // Organisation
    public DeliveryStatus Status { get; set; }
    public int? RouteId { get; set; }
    public bool WithAssembly { get; set; }
    public string? DeliveryNotes { get; set; }
    public string? InternalNotes { get; set; }
    public int? EstimatedDurationMinutes { get; set; }  // ✅ NOUVEAU
    public int? TimeSlotId { get; set; } 
    // Produits
    public List<DeliveryItemModel> Items { get; set; } = new();
}