namespace Berryfy.Domain.Repositories.ShopInterfaces
{
    public static class ShopCategoryPersistenceErrors
    {
        public const string DuplicateSlug = "shop_category_duplicate_slug";
        public const string ForeignKeyViolation = "shop_category_foreign_key_violation";
    }
}
