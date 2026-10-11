using Berryfy.Application.Dtos.CategoryDtos.Requests;
using FluentValidation;

namespace Berryfy.Application.Dtos.CategoryDtos.CategoryValidations
{
    // Limits mirror the shop_categories table: name VARCHAR(150), slug VARCHAR(180).
    internal static class ShopCategoryRules
    {
        public const int NameMaxLength = 150;
        public const int SlugMaxLength = 180;
        public const int DescriptionMaxLength = 1000;
        public const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

        public static bool BeAbsoluteUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    public class CreateShopCategoryRequestValidator : AbstractValidator<CreateShopCategoryRequest>
    {
        public CreateShopCategoryRequestValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(ShopCategoryRules.NameMaxLength)
                .WithMessage($"Name cannot exceed {ShopCategoryRules.NameMaxLength} characters.");

            RuleFor(c => c.Description)
                .MaximumLength(ShopCategoryRules.DescriptionMaxLength)
                .WithMessage($"Description cannot exceed {ShopCategoryRules.DescriptionMaxLength} characters.");

            RuleFor(c => c.ImageUrl)
                .Must(ShopCategoryRules.BeAbsoluteUrl)
                .When(c => !string.IsNullOrWhiteSpace(c.ImageUrl))
                .WithMessage("Invalid URL format.");

            RuleFor(c => c.Slug)
                .MaximumLength(ShopCategoryRules.SlugMaxLength)
                .Matches(ShopCategoryRules.SlugPattern)
                .When(c => !string.IsNullOrWhiteSpace(c.Slug))
                .WithMessage("Slug may only contain lowercase letters, numbers and single hyphens.");

            RuleFor(c => c.ParentCategoryId)
                .GreaterThan(0).When(c => c.ParentCategoryId.HasValue)
                .WithMessage("Parent category id must be a positive number.");

            RuleFor(c => c.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative.");
        }
    }

    public class UpdateShopCategoryRequestValidator : AbstractValidator<UpdateShopCategoryRequest>
    {
        public UpdateShopCategoryRequestValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(ShopCategoryRules.NameMaxLength)
                .WithMessage($"Name cannot exceed {ShopCategoryRules.NameMaxLength} characters.");

            RuleFor(c => c.Description)
                .MaximumLength(ShopCategoryRules.DescriptionMaxLength)
                .WithMessage($"Description cannot exceed {ShopCategoryRules.DescriptionMaxLength} characters.");

            RuleFor(c => c.ImageUrl)
                .Must(ShopCategoryRules.BeAbsoluteUrl)
                .When(c => !string.IsNullOrWhiteSpace(c.ImageUrl))
                .WithMessage("Invalid URL format.");

            RuleFor(c => c.Slug)
                .MaximumLength(ShopCategoryRules.SlugMaxLength)
                .Matches(ShopCategoryRules.SlugPattern)
                .When(c => !string.IsNullOrWhiteSpace(c.Slug))
                .WithMessage("Slug may only contain lowercase letters, numbers and single hyphens.");

            RuleFor(c => c.ParentCategoryId)
                .GreaterThan(0).When(c => c.ParentCategoryId.HasValue)
                .WithMessage("Parent category id must be a positive number.");

            RuleFor(c => c.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative.");
        }
    }
}
