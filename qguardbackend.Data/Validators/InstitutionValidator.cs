using qguardbackend.Data.DTOs;
using FluentValidation;

namespace qguardbackend.Data.Validators
{
    public class InstitutionValidator : AbstractValidator<InstitutionCreateModel>
    {
        public InstitutionValidator()
        {

            RuleFor(x => x.AdminEmail).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("Admin email is required")
                .EmailAddress()
                .WithMessage("Admin email must be valid email address");

            RuleFor(x => x.SenderEmail).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("Sender email is required")
                .EmailAddress()
                .WithMessage("Sender email must be valid email address");

            RuleFor(x => x.Name).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("Institution name is required");

            RuleFor(x => x.Code).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("Institution code is required");

            RuleFor(x => x.DefaultLanguage).Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("Default language is required");

            RuleFor(x => x.PrimaryThemeColor).Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("Primary theme color is required");

            RuleFor(x => x.SecondaryThemeColor).Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("Secondary theme color is required");

        }
    }
}
