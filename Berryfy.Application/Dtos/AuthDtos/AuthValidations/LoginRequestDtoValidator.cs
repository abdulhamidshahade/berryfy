using Berryfy.Application.Dtos.AuthDtos.Requests;
using FluentValidation;

namespace Berryfy.Application.Dtos.AuthDtos.AuthValidations
{
    public class LoginRequestDtoValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(em => em.Email)
                .NotEmpty().WithMessage("Email is required.")
                .NotNull().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Password is required.")
                .NotNull().WithMessage("Password is required.");
        }
    }
}
