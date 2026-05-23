using DropFlow.Shared.Enums;
using DropFlow.Shared.Deliveries;
using FluentValidation;

namespace DropFlow.Application.Validators.Deliveries;

public class CreateDeliveryValidator : AbstractValidator<CreateDeliveryDto>
{
    public CreateDeliveryValidator()
    {
        // Client
        When(x => x.ClientId == null, () =>
        {
           
            RuleFor(x => x.ClientPhone)
                .NotEmpty().WithMessage("Le téléphone est obligatoire")
                .Matches(@"^0[1-9]\d{8}$")
                .WithMessage("Format invalide (ex: 0612345678)");
            
            RuleFor(x => x.ClientFirstName)
                .NotEmpty().WithMessage("Le prénom est obligatoire");
            RuleFor(x => x.ClientLastName)
                .NotEmpty().WithMessage("Le nom est obligatoire");
        });
        
        // Address
        When(x => x.ClientAddressId == null, () =>
        {
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("L'adresse est obligatoire");
            
            RuleFor(x => x.ZipCode)
                .NotEmpty().WithMessage("Le code postal est obligatoire")
                .Matches(@"^\d{5}$")
                .WithMessage("Code postal invalide (5 chiffres)");
            
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("La ville est obligatoire");
        });
        
        // Store
        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("Le magasin est obligatoire");
        
        // Price
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Le prix doit être > 0");
        
        // Scheduled Date
        RuleFor(x => x.ScheduledDate)
            .NotNull()
            .When(x => x.Status != DeliveryStatus.ToBePlanned)
            .WithMessage("La date est obligatoire si statut ? 'À Planifier'");
        
        // Items
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Au moins un produit est obligatoire");
        
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Designation)
                .NotEmpty()
                .WithMessage("La désignation de l'article est obligatoire")
                .MaximumLength(200)
                .WithMessage("La désignation ne peut pas dépasser 200 caractères");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("La quantité doit être supérieure à 0")
                .LessThan(10000)
                .WithMessage("La quantité ne peut pas dépasser 10 000");

            item.When(x => !string.IsNullOrEmpty(x.Reference), () =>
            {
                item.RuleFor(x => x.Reference)
                    .MaximumLength(100)
                    .WithMessage("La référence ne peut pas dépasser 100 caractères");
            });

            item.When(x => !string.IsNullOrEmpty(x.Information), () =>
            {
                item.RuleFor(x => x.Information)
                    .MaximumLength(500)
                    .WithMessage("Les informations ne peuvent pas dépasser 500 caractères");
            });
        });
        
        When(x => x.ScheduledDate.HasValue, () =>
        {
            RuleFor(x => x.EstimatedDurationMinutes)
                .NotNull()
                .WithMessage("La durée estimée de prestation est obligatoire lorsque la date de livraison est définie")
                .GreaterThan(0)
                .WithMessage("La durée doit être supérieure à 0")
                .LessThanOrEqualTo(480)
                .WithMessage("La durée ne peut pas dépasser 8 heures (480 minutes)");
        });

        // ? Si Confirmed ou InProgress, date OBLIGATOIRE (et donc durée aussi via règle précédente)
        When(x => x.Status is DeliveryStatus.Confirmed or DeliveryStatus.InProgress, () =>
        {
            RuleFor(x => x.ScheduledDate)
                .NotNull()
                .WithMessage("La date de livraison est obligatoire pour une livraison confirmée ou en cours");
        });
    }
}