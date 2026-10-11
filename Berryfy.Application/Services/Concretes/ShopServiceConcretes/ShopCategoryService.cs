using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Berryfy.Domain.Repositories.ShopInterfaces;

namespace Berryfy.Application.Services.Concretes.ShopServiceConcretes
{
    public class ShopCategoryService : IShopCategoryService
    {
        private const int SlugMaxLength = 180;

        private readonly IShopCategoryRepository _shopCategoryRepository;
        private readonly IShopRepository _shopRepository;

        public ShopCategoryService(IShopCategoryRepository shopCategoryRepository, IShopRepository shopRepository)
        {
            _shopCategoryRepository = shopCategoryRepository;
            _shopRepository = shopRepository;
        }

        public async Task<ApplicationResponse<IEnumerable<ShopCategoryResponse>>> GetAllAsync(int shopId, bool includeInactive = false)
        {
            if (shopId <= 0)
                return Fail<IEnumerable<ShopCategoryResponse>>(ShopCategoryErrors.InvalidShopId);

            if (!await ShopExistsAsync(shopId))
                return Fail<IEnumerable<ShopCategoryResponse>>(ShopCategoryErrors.ShopNotFound);

            var result = await _shopCategoryRepository.GetAllByShopIdAsync(shopId, includeInactive);

            return Ok(result.Value.Select(ShopCategoryResponse.MapFromShopCategory), result.Message);
        }

        public async Task<ApplicationResponse<ShopCategoryResponse>> GetByIdAsync(int shopId, int id)
        {
            var validationError = ValidateIds(shopId, id);
            if (validationError != null)
                return Fail<ShopCategoryResponse>(validationError);

            var result = await _shopCategoryRepository.GetByIdAsync(shopId, id);

            return result.Value == null
                ? Fail<ShopCategoryResponse>(ShopCategoryErrors.NotFound)
                : Ok(ShopCategoryResponse.MapFromShopCategory(result.Value), result.Message);
        }

        public async Task<ApplicationResponse<ShopCategoryResponse>> GetBySlugAsync(int shopId, string slug)
        {
            if (shopId <= 0)
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.InvalidShopId);

            if (string.IsNullOrWhiteSpace(slug))
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.InvalidSlug);

            var result = await _shopCategoryRepository.GetBySlugAsync(shopId, slug.Trim().ToLowerInvariant());

            return result.Value == null
                ? Fail<ShopCategoryResponse>(ShopCategoryErrors.NotFound)
                : Ok(ShopCategoryResponse.MapFromShopCategory(result.Value), result.Message);
        }

        public async Task<ApplicationResponse<ShopCategoryResponse>> CreateAsync(int shopId, CreateShopCategoryRequest request)
        {
            if (shopId <= 0)
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.InvalidShopId);

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.InvalidRequest);

            if (!await ShopExistsAsync(shopId))
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.ShopNotFound);

            var slug = ResolveSlug(request.Slug, request.Name);
            if (slug == null)
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.InvalidSlug);

            if ((await _shopCategoryRepository.ExistsBySlugAsync(shopId, slug)).Value)
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.SlugAlreadyExists);

            if (request.ParentCategoryId.HasValue &&
                !(await _shopCategoryRepository.ExistsByIdAsync(shopId, request.ParentCategoryId.Value)).Value)
            {
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.ParentNotFound);
            }

            var category = CreateShopCategoryRequest.MapToShopCategory(shopId, request, slug);
            var created = await _shopCategoryRepository.CreateAsync(category);

            if (!created.IsSuccess || created.Value == null)
                return Fail<ShopCategoryResponse>(MapPersistenceError(created.Message));

            return Ok(ShopCategoryResponse.MapFromShopCategory(created.Value), created.Message);
        }

        public async Task<ApplicationResponse<ShopCategoryResponse>> UpdateAsync(int shopId, int id, UpdateShopCategoryRequest request)
        {
            var validationError = ValidateIds(shopId, id);
            if (validationError != null)
                return Fail<ShopCategoryResponse>(validationError);

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.InvalidRequest);

            if (!(await _shopCategoryRepository.ExistsByIdAsync(shopId, id)).Value)
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.NotFound);

            var slug = ResolveSlug(request.Slug, request.Name);
            if (slug == null)
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.InvalidSlug);

            if ((await _shopCategoryRepository.ExistsBySlugAsync(shopId, slug, excludeId: id)).Value)
                return Fail<ShopCategoryResponse>(ShopCategoryErrors.SlugAlreadyExists);

            if (request.ParentCategoryId.HasValue)
            {
                var parentId = request.ParentCategoryId.Value;

                if (!(await _shopCategoryRepository.ExistsByIdAsync(shopId, parentId)).Value)
                    return Fail<ShopCategoryResponse>(ShopCategoryErrors.ParentNotFound);

                if ((await _shopCategoryRepository.IsSelfOrDescendantAsync(shopId, ancestorId: id, candidateId: parentId)).Value)
                    return Fail<ShopCategoryResponse>(ShopCategoryErrors.ParentCycle);
            }

            var category = UpdateShopCategoryRequest.MapToShopCategory(shopId, id, request, slug);
            var updated = await _shopCategoryRepository.UpdateAsync(category);

            if (!updated.IsSuccess || updated.Value == null)
                return Fail<ShopCategoryResponse>(MapPersistenceError(updated.Message, ShopCategoryErrors.NotFound));

            return Ok(ShopCategoryResponse.MapFromShopCategory(updated.Value), updated.Message);
        }

        public async Task<ApplicationResponse<bool>> DeleteAsync(int shopId, int id)
        {
            var validationError = ValidateIds(shopId, id);
            if (validationError != null)
                return Fail<bool>(validationError);

            if (!(await _shopCategoryRepository.ExistsByIdAsync(shopId, id)).Value)
                return Fail<bool>(ShopCategoryErrors.NotFound);

            if ((await _shopCategoryRepository.HasChildCategoriesAsync(shopId, id)).Value)
                return Fail<bool>(ShopCategoryErrors.HasChildren);

            if ((await _shopCategoryRepository.GetProductCountByShopCategoryIdAsync(shopId, id)).Value > 0)
                return Fail<bool>(ShopCategoryErrors.HasProducts);

            var deleted = await _shopCategoryRepository.DeleteAsync(shopId, id);

            if (!deleted.IsSuccess)
                return Fail<bool>(MapPersistenceError(deleted.Message));

            return deleted.Value
                ? Ok(true, deleted.Message)
                : Fail<bool>(ShopCategoryErrors.NotFound);
        }

        public async Task<ApplicationResponse<bool>> ExistsAsync(int shopId, int id)
        {
            var validationError = ValidateIds(shopId, id);
            if (validationError != null)
                return Fail<bool>(validationError);

            var exists = await _shopCategoryRepository.ExistsByIdAsync(shopId, id);
            return Ok(exists.Value, exists.Message);
        }

        public async Task<ApplicationResponse<List<ProductResponse>>> GetProductsAsync(int shopId, int id)
        {
            var validationError = ValidateIds(shopId, id);
            if (validationError != null)
                return Fail<List<ProductResponse>>(validationError);

            if (!(await _shopCategoryRepository.ExistsByIdAsync(shopId, id)).Value)
                return Fail<List<ProductResponse>>(ShopCategoryErrors.NotFound);

            var products = await _shopCategoryRepository.GetProductsByShopCategoryIdAsync(shopId, id);
            return Ok(ProductResponse.MapFromProduct(products.Value), products.Message);
        }

        public async Task<ApplicationResponse<int>> GetProductCountAsync(int shopId, int id)
        {
            var validationError = ValidateIds(shopId, id);
            if (validationError != null)
                return Fail<int>(validationError);

            if (!(await _shopCategoryRepository.ExistsByIdAsync(shopId, id)).Value)
                return Fail<int>(ShopCategoryErrors.NotFound);

            var count = await _shopCategoryRepository.GetProductCountByShopCategoryIdAsync(shopId, id);
            return Ok(count.Value, count.Message);
        }

        #region Helpers

        private async Task<bool> ShopExistsAsync(int shopId)
        {
            var result = await _shopRepository.ExistsAsync(shopId);
            return result.IsSuccess && result.Value;
        }

        private static string? ValidateIds(int shopId, int id)
        {
            if (shopId <= 0) return ShopCategoryErrors.InvalidShopId;
            if (id <= 0) return ShopCategoryErrors.InvalidCategoryId;
            return null;
        }

        private static string MapPersistenceError(string? repositoryMessage, string fallback = ShopCategoryErrors.InvalidRequest)
        {
            return repositoryMessage switch
            {
                ShopCategoryPersistenceErrors.DuplicateSlug => ShopCategoryErrors.SlugAlreadyExists,
                ShopCategoryPersistenceErrors.ForeignKeyViolation => ShopCategoryErrors.HasProducts,
                _ => fallback
            };
        }

        private static string? ResolveSlug(string? requestedSlug, string name)
        {
            var slug = Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);
            return string.IsNullOrEmpty(slug) ? null : slug;
        }

        private static string Slugify(string value)
        {
            var normalized = value.Trim()
                .Replace('ı', 'i').Replace('İ', 'I')
                .Replace("ß", "ss")
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            var slug = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            slug = Regex.Replace(slug, "[^a-z0-9]+", "-").Trim('-');

            if (slug.Length > SlugMaxLength)
                slug = slug[..SlugMaxLength].TrimEnd('-');

            return slug;
        }

        private static ApplicationResponse<T> Ok<T>(T value, string? message = null) => new()
        {
            IsSuccess = true,
            Value = value,
            SuccessMessage = message
        };

        private static ApplicationResponse<T> Fail<T>(string error) => new()
        {
            IsSuccess = false,
            ErrorMessage = error
        };

        #endregion
    }
}