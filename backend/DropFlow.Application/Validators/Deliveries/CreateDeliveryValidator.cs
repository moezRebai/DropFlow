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
                .NotEmpty().WithMessage("Le t�l�phone est obligatoire")
                .Matches(@"^0[1-9]\d{8}$")
                .WithMessage("Format invalide (ex: 0612345678)");
            
            RuleFor(x => x.ClientFirstName)
                .NotEmpty().WithMessage("Le pr�nom est obligatoire");
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
            .GreaterThan(0).WithMessage("Le prix doit �tre > 0");
        
        // Scheduled Date
        RuleFor(x => x.ScheduledDate)
            .NotNull()
            .When(x => x.Status != DeliveryStatus.ToBePlanned)
            .WithMessage("La date est obligatoire si statut ? '� Planifier'");
        
        // Items
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Au moins un produit est obligatoire");
        
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Designation)
                .NotEmpty()
                .WithMessage("La d�signation de l'article est obligatoire")
                .MaximumLength(200)
                .WithMessage("La d�signation ne peut pas d�passer 200 caract�res");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("La quantit� doit �tre sup�rieure � 0")
                .LessThan(10000)
                .WithMessage("La quantit� ne peut pas d�passer 10 000");

            item.When(x => !string.IsNullOrEmpty(x.Reference), () =>
            {
                item.RuleFor(x => x.Reference)
                    .MaximumLength(100)
                    .WithMessage("La r�f�rence ne peut pas d�passer 100 caract�res");
            });

            item.When(x => !string.IsNullOrEmpty(x.Information), () =>
            {
                item.RuleFor(x => x.Information)
                    .MaximumLength(500)
                    .WithMessage("Les informations ne peuvent pas d�passer 500 caract�res");
            });
        });
        
        When(x => x.EstimatedDurationMinutes.HasValue, () =>
        {
            RuleFor(x => x.EstimatedDurationMinutes)
                .GreaterThan(0)
                .WithMessage("La durée doit être supérieure à 0")
                .LessThanOrEqualTo(480)
                .WithMessage("La durée ne peut pas dépasser 8 heures (480 minutes)");
        });

        When(x => x.Status is DeliveryStatus.Confirmed or DeliveryStatus.InProgress, () =>
        {
            RuleFor(x => x.ScheduledDate)
                .NotNull()
                .WithMessage("La date de livraison est obligatoire pour une livraison confirmée ou en cours");
        });

        When(x => x.ScheduledDate.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .NotEqual(DeliveryStatus.ToBePlanned)
                .WithMessage("Une livraison avec une date planifiée ne peut pas avoir le statut 'À planifier'");
        });
    }
}