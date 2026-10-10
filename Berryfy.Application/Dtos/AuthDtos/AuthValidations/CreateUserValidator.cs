using Berryfy.Application.Dtos.AuthDtos.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Berryfy.Application.Dtos.AuthDtos.AuthValidations
{
    public class CreateUserValidator : AbstractValidator<CreateUser>
    {
        public CreateUserValidator()
        {
            RuleFor(fn => fn.FirstName)
                .NotNull().WithMessage("First name is required.")
                .NotEmpty().WithMessage("First name is required.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("First name must contain only letters.")
                .MinimumLength(1).WithMessage("First name must be at least 1 characters long.")
                .MaximumLength(100).WithMessage("First name must be at most 100 characters long.");

            RuleFor(ln => ln.LastName)
                .NotNull().WithMessage("Last name is required.")
                .NotEmpty().WithMessage("Last name is required.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("Last name must contain only letters.")
                .MinimumLength(1).WithMessage("Last name must be at least 1 characters long.")
                .MaximumLength(100).WithMessage("Last name must be at most 100 characters long.");

            RuleFor(un => un.UserName)
                .NotNull().WithMessage("Username is required.")
                .NotEmpty().WithMessage("Username is required.")
                .Matches(@"^[\p{L}0-9_-]+$").WithMessage("Username may include letters, numbers, underscores, and hyphens.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(30).WithMessage("Username must be at most 30 characters long.");

            RuleFor(e => e.Email)
                .NotNull().WithMessage("Email is required.")
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(p => p.Password)
                .NotNull().WithMessage("Password is required.")
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(128).WithMessage("Password must be at most 128 characters long.")
                .Must(password => ContainsUpperCase(password)).WithMessage("Password should contain at least one uppercase letter.")
                .Must(password => ContainsDigits(password)).WithMessage("Password should contain at least one digit.")
                .Must(password => ContainsSpecial(password)).WithMessage("Password should contain at least one special character.");

            RuleFor(ec => ec.EmailConfirmed)
                .NotNull().WithMessage("Email confirmation status is required.")
                .NotEmpty().WithMessage("Email confirmation status is required.");
        }

        //special functions.
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