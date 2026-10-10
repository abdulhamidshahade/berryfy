using Berryfy.Application.Dtos.AuthDtos.Requests;
using FluentValidation;

namespace Berryfy.Application.Dtos.AuthDtos.AuthValidations
{
    public class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
    {
        public ResendConfirmationRequestValidator()
        {
            RuleFor(e => e.Email)
                .NotNull().WithMessage("Email is required.")
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
        }
    }
}
