using DropFlow.Shared.Clients;
using FluentValidation;

namespace DropFlow.Application.Validators.Clients;

public class CreateClientAddressValidator : AbstractValidator<CreateClientAddressDto>
{
    public CreateClientAddressValidator()
    {
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("L'adresse est obligatoire")
            .MaximumLength(500);

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Le code postal est obligatoire")
            .Matches(@"^\d{5}$")
            .WithMessage("Le code postal doit contenir 5 chiffres");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("La ville est obligatoire")
            .MaximumLength(100);
    }
}