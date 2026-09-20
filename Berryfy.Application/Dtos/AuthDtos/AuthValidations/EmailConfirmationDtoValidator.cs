using Berryfy.Application.Dtos.AuthDtos.Requests;
using FluentValidation;

namespace Berryfy.Application.Dtos.AuthDtos.AuthValidations
{
    public class EmailConfirmationDtoValidator : AbstractValidator<EmailConfirmation>
    {
        public EmailConfirmationDtoValidator()
        {
            RuleFor(em => em.Email)
                .NotNull().WithMessage("Email is required.")
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(em => em.Code)
                .NotNull().WithMessage("Confirmation code is required.")
                .NotEmpty().WithMessage("Confirmation code is required.")
                .Length(6).WithMessage("Confirmation code must be exactly 6 digits.")
                .Matches(@"^\d{6}$").WithMessage("Confirmation code must contain only digits.");
        }
    }
}
