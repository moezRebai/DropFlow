using System.ComponentModel.DataAnnotations;

namespace DropFlow.Shared.Enums;

public enum DeliveryType
{
    [Display(Name = "Standard")]
    Standard = 0,

    [Display(Name = "Urgente")]
    Urgent = 1
}
