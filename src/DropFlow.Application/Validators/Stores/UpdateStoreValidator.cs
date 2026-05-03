using DropFlow.Shared.Stores;
using FluentValidation;

namespace DropFlow.Application.Validators.Stores;

public class UpdateStoreValidator : AbstractValidator<UpdateStoreDto>
{
    public UpdateStoreValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom du magasin est obligatoire")
            .MaximumLength(200).WithMessage("Le nom ne peut pas dépasser 200 caractères");

        When(x => !string.IsNullOrEmpty(x.Phone), () =>
        {
            RuleFor(x => x.Phone)
                .Matches(@"^0[1-9]\d{8}$")
                .WithMessage("Le téléphone doit être au format français (ex: 0612345678)");
        });

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("L'email n'est pas valide");
        });
    }
}