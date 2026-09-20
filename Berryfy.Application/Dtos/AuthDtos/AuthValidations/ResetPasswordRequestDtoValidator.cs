using Berryfy.Application.Dtos.AuthDtos.Requests;
using FluentValidation;

namespace Berryfy.Application.Dtos.AuthDtos.AuthValidations
{
    public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestDtoValidator()
        {
            RuleFor(fn => fn.Email)
                .NotNull().WithMessage("Email is required.")
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(em => em.Token)
                .NotNull().WithMessage("Token is required.")
                .NotEmpty().WithMessage("Token is required.");

            RuleFor(fn => fn.NewPassword)
                .NotNull().WithMessage("Password is required.")
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(2).WithMessage("Password must be at least 2 characters long.")
                .MaximumLength(50).WithMessage("Password must be at most 50 characters long.")
                .Must(pass => ContainsUpperCase(pass)).WithMessage("Password should contains at least one uppercase letter.")
                .Must(pass => ContainsDigits(pass)).WithMessage("Password should contains at least one digit.")
                .Must(pass => ContainsSpecial(pass)).WithMessage("Password should contains at least one special character.");

            RuleFor(fn => fn.ConfirmPassword)
                .NotNull().WithMessage("Confirm password is required.")
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(fn => fn.NewPassword).WithMessage("Confirm password must match the new password.");
        }

        private bool ContainsUpperCase(string password)
        {
            return password.Any(ch => char.IsUpper(ch));
        }
        private bool ContainsDigits(string password)
        {
            return password.Any(ch => char.IsDigit(ch));
        }
        private bool ContainsSpecial(string password)
        {
            return password.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch));
        }
    }
}
