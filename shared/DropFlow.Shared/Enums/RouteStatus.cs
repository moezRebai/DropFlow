using System.ComponentModel.DataAnnotations;

namespace DropFlow.Shared.Enums;

public enum RouteStatus
{
    [Display(Name = "Brouillon")]
    Draft = 0,

    [Display(Name = "Confirmée")]
    Confirmed = 1,

    [Display(Name = "En cours")]
    InProgress = 2,

    [Display(Name = "Terminée")]
    Completed = 3,

    [Display(Name = "Annulée")]
    Cancelled = 4
}
