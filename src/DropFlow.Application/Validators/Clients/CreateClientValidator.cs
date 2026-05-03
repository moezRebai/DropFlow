using DropFlow.Shared.Clients;
using FluentValidation;

namespace DropFlow.Application.Validators.Clients;

public class CreateClientValidator : AbstractValidator<CreateClientDto>
{
    public CreateClientValidator()
    {
        // Validation conditionnelle selon le type
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est obligatoire pour un particulier")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est obligatoire pour un particulier")
            .MaximumLength(100);

        // Phone obligatoire
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Le téléphone est obligatoire")
            .Matches(@"^0[1-9]\d{8}$")
            .WithMessage("Le téléphone doit être au format français (ex: 0612345678)");

        // Email optionnel mais validé si présent
        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("L'email n'est pas valide");
        });

        // Validation de l'adresse
        RuleFor(x => x.Address)
            .NotNull().WithMessage("L'adresse est obligatoire")
            .SetValidator(new CreateClientAddressValidator());
    }
}