using Berryfy.Application.Dtos.AuthDtos.Requests;
using FluentValidation;

namespace Berryfy.Application.Dtos.AuthDtos.AuthValidations
{
    public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(fn => fn.FirstName)
                .NotNull().WithMessage("First name is required.")
                .NotEmpty().WithMessage("First name is required.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("First name must contain only letters.")
                .MinimumLength(1).WithMessage("First name must be at least 2 characters long.")
                .MaximumLength(100).WithMessage("First name must be at most 100 characters long.");

            RuleFor(fn => fn.LastName)
                .NotNull().WithMessage("Last name is required.")
                .NotEmpty().WithMessage("Last name is required.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("Last name must contain only letters.")
                .MinimumLength(1).WithMessage("Last name must be at least 2 characters long.")
                .MaximumLength(100).WithMessage("Last name must be at most 100 characters long.");

            RuleFor(fn => fn.UserName)
                .NotNull().WithMessage("Username is required.")
                .NotEmpty().WithMessage("Username is required.")
                .Matches(@"^[\p{L}0-9_-]+$").WithMessage("Username may include letters, numbers, underscores, and hyphens.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(30).WithMessage("Username must be at most 30 characters long.");

            RuleFor(fn => fn.Email)
                .NotNull().WithMessage("Email is required.")
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(fn => fn.Password)
                .NotNull().WithMessage("Password is required.")
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                .MaximumLength(50).WithMessage("Password must be at most 50 characters long.")
                .Must(pass => ContainsUpperCase(pass)).WithMessage("Password should contain at least one uppercase letter.")
                .Must(pass => ContainsDigits(pass)).WithMessage("Password should contain at least one digit.")
                .Must(pass => ContainsSpecial(pass)).WithMessage("Password should contain at least one special character.");
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
