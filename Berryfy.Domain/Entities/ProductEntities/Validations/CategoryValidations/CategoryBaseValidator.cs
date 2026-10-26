using FluentValidation;

namespace Berryfy.Domain.Entities.ProductEntities.Validations.CategoryValidations
{
    public class CategoryBaseValidator<T> : AbstractValidator<T> where T : Category
    {
        public CategoryBaseValidator()
        {
            RuleFor(n => n.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

            RuleFor(p => p.ImageUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(p => !string.IsNullOrEmpty(p.ImageUrl))
                .WithMessage("Invalid URL format.");
        }
    }
}
