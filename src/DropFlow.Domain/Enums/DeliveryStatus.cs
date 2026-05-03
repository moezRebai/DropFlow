using System.ComponentModel.DataAnnotations;

namespace DropFlow.Domain.Enums;

public enum DeliveryStatus
{
    [Display(Name = "À Planifier")]
    ToBePlanned = 0,
    
    [Display(Name = "Confirmée")]
    Confirmed = 1,
    
    [Display(Name = "En cours")]
    InProgress = 2,
    
    [Display(Name = "Livrée")]
    Delivered = 3,
    
    [Display(Name = "Annulée")]
    Canceled = 4
}