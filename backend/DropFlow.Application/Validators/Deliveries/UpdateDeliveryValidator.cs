using DropFlow.Shared.Enums;
using DropFlow.Shared.Deliveries;
using FluentValidation;

namespace DropFlow.Application.Validators.Deliveries;

public class UpdateDeliveryValidator : AbstractValidator<UpdateDeliveryDto>
{
    public UpdateDeliveryValidator()
    {
        // ----------------------------------------------------------------
        // STORE (obligatoire)
        // ----------------------------------------------------------------
        
        RuleFor(x => x.StoreId)
            .GreaterThan(0)
            .WithMessage("Le magasin est obligatoire");

        // ----------------------------------------------------------------
        // CLIENT (conditionnel si nouveau client)
        // ----------------------------------------------------------------
        
        // Si ClientId est null, on doit cr�er un nouveau client
        When(x => x.ClientId == null, () =>
        {
            RuleFor(x => x.ClientFirstName)
                .NotEmpty()
                .WithMessage("Le pr�nom est obligatoire pour un particulier")
                .MaximumLength(100)
                .WithMessage("Le pr�nom ne peut pas d�passer 100 caract�res");

            RuleFor(x => x.ClientLastName)
                .NotEmpty()
                .WithMessage("Le nom est obligatoire pour un particulier")
                .MaximumLength(100)
                .WithMessage("Le nom ne peut pas d�passer 100 caract�res");

            // Phone obligatoire si nouveau client
            RuleFor(x => x.ClientPhone)
                .NotEmpty()
                .WithMessage("Le t�l�phone est obligatoire pour un nouveau client")
                .Matches(@"^0[1-9]\d{8}$")
                .WithMessage("Le t�l�phone doit �tre au format fran�ais (ex: 0612345678)");

            // Email optionnel mais valid� si pr�sent
            When(x => !string.IsNullOrEmpty(x.ClientEmail), () =>
            {
                RuleFor(x => x.ClientEmail)
                    .EmailAddress()
                    .WithMessage("L'email n'est pas valide")
                    .MaximumLength(100)
                    .WithMessage("L'email ne peut pas d�passer 100 caract�res");
            });
        });

        // ----------------------------------------------------------------
        // ADRESSE (conditionnel si nouvelle adresse)
        // ----------------------------------------------------------------
        
        // Si ClientAddressId est null, on doit cr�er une nouvelle adresse
        When(x => x.ClientAddressId == null, () =>
        {
            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("L'adresse est obligatoire")
                .MaximumLength(500)
                .WithMessage("L'adresse ne peut pas d�passer 500 caract�res");

            RuleFor(x => x.ZipCode)
                .NotEmpty()
                .WithMessage("Le code postal est obligatoire")
                .Matches(@"^\d{5}$")
                .WithMessage("Le code postal doit contenir 5 chiffres");

            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("La ville est obligatoire")
                .MaximumLength(100)
                .WithMessage("La ville ne peut pas d�passer 100 caract�res");

            When(x => !string.IsNullOrEmpty(x.AddressComplement), () =>
            {
                RuleFor(x => x.AddressComplement)
                    .MaximumLength(200)
                    .WithMessage("Le compl�ment d'adresse ne peut pas d�passer 200 caract�res");
            });
        });

        // ----------------------------------------------------------------
        // PRIX
        // ----------------------------------------------------------------
        
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Le prix doit �tre sup�rieur � 0")
            .LessThan(1000000)
            .WithMessage("Le prix ne peut pas d�passer 1 000 000�");

        When(x => x.ClientPaymentAmount.HasValue, () =>
        {
            RuleFor(x => x.ClientPaymentAmount!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Le montant pay� par le client ne peut pas �tre n�gatif")
                .LessThanOrEqualTo(x => x.Price)
                .WithMessage("Le montant pay� par le client ne peut pas d�passer le prix total");
        });

        When(x => x.StorePaymentAmount.HasValue, () =>
        {
            RuleFor(x => x.StorePaymentAmount!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Le montant pay� par le magasin ne peut pas �tre n�gatif")
                .LessThanOrEqualTo(x => x.Price)
                .WithMessage("Le montant pay� par le magasin ne peut pas d�passer le prix total");
        });

        // V�rification: ClientPayment + StorePayment <= Price
        RuleFor(x => x)
            .Must(dto => 
            {
                var clientPayment = dto.ClientPaymentAmount ?? 0;
                var storePayment = dto.StorePaymentAmount ?? 0;
                return clientPayment + storePayment <= dto.Price;
            })
            .WithMessage("La somme des paiements client et magasin ne peut pas d�passer le prix total")
            .When(x => x.ClientPaymentAmount.HasValue || x.StorePaymentAmount.HasValue);

        // ----------------------------------------------------------------
        // DATE
        // ----------------------------------------------------------------
        
        // Date obligatoire si statut n'est pas "� planifier"
        When(x => x.Status != DeliveryStatus.ToBePlanned, () =>
        {
            RuleFor(x => x.ScheduledDate)
                .NotNull()
                .WithMessage("La date de livraison est obligatoire pour ce statut");
        });

        // ----------------------------------------------------------------
        // STATUT
        // ----------------------------------------------------------------
        
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Le statut n'est pas valide");

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
        
        // ----------------------------------------------------------------
        // ITEMS (Articles)
        // ----------------------------------------------------------------
        
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Au moins un article est obligatoire");

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

        // ----------------------------------------------------------------
        // NOTES
        // ----------------------------------------------------------------
        
        When(x => !string.IsNullOrEmpty(x.DeliveryNotes), () =>
        {
            RuleFor(x => x.DeliveryNotes)
                .MaximumLength(2000)
                .WithMessage("Les notes de livraison ne peuvent pas d�passer 2000 caract�res");
        });

        When(x => !string.IsNullOrEmpty(x.InternalNotes), () =>
        {
            RuleFor(x => x.InternalNotes)
                .MaximumLength(2000)
                .WithMessage("Les notes internes ne peuvent pas d�passer 2000 caract�res");
        });

        When(x => !string.IsNullOrEmpty(x.FileNumber), () =>
        {
            RuleFor(x => x.FileNumber)
                .MaximumLength(50)
                .WithMessage("Le num�ro de dossier ne peut pas d�passer 50 caract�res");
        });
    }
}