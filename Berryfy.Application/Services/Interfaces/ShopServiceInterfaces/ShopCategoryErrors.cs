namespace Berryfy.Application.Services.Interfaces.ShopServiceInterfaces
{
    public static class ShopCategoryErrors
    {
        public const string InvalidShopId = "Shop id must be a positive number";
        public const string InvalidCategoryId = "Shop category id must be a positive number";
        public const string InvalidRequest = "Invalid shop category request";
        public const string InvalidSlug = "A valid slug could not be generated; please provide one explicitly";

        public const string ShopNotFound = "Shop not found";
        public const string NotFound = "Shop category not found";
        public const string ParentNotFound = "Parent category not found in this shop";

        public const string SlugAlreadyExists = "A category with this slug already exists in this shop";
        public const string ParentCycle = "A category cannot be its own parent or be nested under one of its descendants";
        public const string HasChildren = "Shop category has child categories; move or delete them first";
        public const string HasProducts = "Shop category still has products assigned; reassign them first";
    }
}
