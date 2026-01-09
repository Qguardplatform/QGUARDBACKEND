using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using FluentValidation;

namespace qguardbackend.Data.Validators
{
    public class CandidateValidator : AbstractValidator<CandidateRequestDto>
    {
        public CandidateValidator()
        {

            RuleFor(x => x.Email).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage(" email is required")
                .EmailAddress()
                .WithMessage(" email must be valid email address");

            RuleFor(x => x.FirstName).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("First name is required");

            RuleFor(x => x.LastName).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("Last name is required");
        }
    }

    public class UpdateCandidateValidator : AbstractValidator<UpdateCandidateRequestDto>
    {
        public UpdateCandidateValidator()
        {
            RuleFor(x => x.InstitutionId).Cascade(CascadeMode.Stop).InclusiveBetween(1, long.MaxValue)
                            .WithMessage("Must be a valid InstitutionId above 0");

            RuleFor(x => x.FirstName).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("First name is required");


            RuleFor(x => x.LastName).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .NotNull()
                .WithMessage("Last name is required");
        }
    }    
}