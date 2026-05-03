using System.ComponentModel.DataAnnotations;

namespace DropFlow.Domain.Enums;

public enum TeamMemberRole
{
    [Display(Name = "Chauffeur principal")]
    MainDriver = 1,
    
    [Display(Name = "Aide-livreur")]
    Helper = 2
}