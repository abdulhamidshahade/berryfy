using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Berryfy.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Berryfy.API.Controllers
{
    [ApiController]
    [Route("api/shop-categories")]
    public class ShopCategoriesController : BaseController
    {
        private readonly IShopCategoryService _shopCategoryService;

        public ShopCategoriesController(IShopCategoryService shopCategoryService)
        {
            _shopCategoryService = shopCategoryService;
        }

        [HttpGet("{shopId:int}")]
        [AllowAnonymous]
        [EnableRateLimiting("DefaultPolicy")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ShopCategoryResponse>>>> GetAll(
            int shopId, [FromQuery] bool includeInactive = false)
        {
            var result = await _shopCategoryService.GetAllAsync(shopId, includeInactive && IsAdmin());

            return result.IsSuccess
                ? Success(result.Value, result.SuccessMessage ?? "Shop categories retrieved successfully")
                : Failure<IEnumerable<ShopCategoryResponse>>(result.ErrorMessage);
        }

        [HttpGet("{shopId:int}/{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<ShopCategoryResponse>>> GetById(int shopId, int id)
        {
            var result = await _shopCategoryService.GetByIdAsync(shopId, id);
            return ToVisibleCategoryResult(result);
        }

        [HttpGet("{shopId:int}/slug/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<ShopCategoryResponse>>> GetBySlug(int shopId, string slug)
        {
            var result = await _shopCategoryService.GetBySlugAsync(shopId, slug);
            return ToVisibleCategoryResult(result);
        }

        [HttpPost("{shopId:int}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<ShopCategoryResponse>>> Create(
            int shopId, [FromBody] CreateShopCategoryRequest request)
        {
            var result = await _shopCategoryService.CreateAsync(shopId, request);

            if (!result.IsSuccess || result.Value == null)
                return Failure<ShopCategoryResponse>(result.ErrorMessage);

            var response = new ApiResponse<ShopCategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                StatusMessage = result.SuccessMessage ?? "Shop category created successfully",
                Data = result.Value,
                Errors = new List<string>()
            };

            return CreatedAtAction(nameof(GetById), new { shopId, id = result.Value.Id }, response);
        }

        [HttpPut("{shopId:int}/{id:int}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<ShopCategoryResponse>>> Update(
            int shopId, int id, [FromBody] UpdateShopCategoryRequest request)
        {
            var result = await _shopCategoryService.UpdateAsync(shopId, id, request);

            return result.IsSuccess
                ? Success(result.Value, result.SuccessMessage ?? "Shop category updated successfully")
                : Failure<ShopCategoryResponse>(result.ErrorMessage);
        }

        [HttpDelete("{shopId:int}/{id:int}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int shopId, int id)
        {
            var result = await _shopCategoryService.DeleteAsync(shopId, id);

            return result.IsSuccess
                ? Success(true, result.SuccessMessage ?? "Shop category deleted successfully")
                : Failure<bool>(result.ErrorMessage);
        }

        [HttpGet("{shopId:int}/{id:int}/exists")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> Exists(int shopId, int id)
        {
            var result = await _shopCategoryService.ExistsAsync(shopId, id);

            if (!result.IsSuccess)
                return Failure<bool>(result.ErrorMessage);

            return result.Value
                ? Success(true, "Shop category exists")
                : NotFound(Build(false, StatusCodes.Status404NotFound, ShopCategoryErrors.NotFound, false));
        }

        [HttpGet("{shopId:int}/{id:int}/products")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> GetProducts(int shopId, int id)
        {
            var result = await _shopCategoryService.GetProductsAsync(shopId, id);

            return result.IsSuccess
                ? Success(result.Value, result.SuccessMessage ?? "Products retrieved successfully")
                : Failure<List<ProductResponse>>(result.ErrorMessage);
        }

        [HttpGet("{shopId:int}/{id:int}/products/count")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<int>>> GetProductCount(int shopId, int id)
        {
            var result = await _shopCategoryService.GetProductCountAsync(shopId, id);

            return result.IsSuccess
                ? Success(result.Value, result.SuccessMessage ?? "Product count retrieved successfully")
                : Failure<int>(result.ErrorMessage);
        }

        #region Helpers

        private bool IsAdmin() =>
            User.IsInRole(RoleConstants.Admin) || User.IsInRole(RoleConstants.SuperAdmin);

        private ActionResult<ApiResponse<ShopCategoryResponse>> ToVisibleCategoryResult(
            ApplicationResponse<ShopCategoryResponse> result)
        {
            if (!result.IsSuccess || result.Value == null)
                return Failure<ShopCategoryResponse>(result.ErrorMessage);

            if (!result.Value.IsActive && !IsAdmin())
                return Failure<ShopCategoryResponse>(ShopCategoryErrors.NotFound);

            return Success(result.Value, result.SuccessMessage ?? "Shop category retrieved successfully");
        }

        private ActionResult<ApiResponse<T>> Success<T>(T? data, string message) =>
            Ok(Build(true, StatusCodes.Status200OK, message, data));

        private ActionResult<ApiResponse<T>> Failure<T>(string? error)
        {
            var message = error ?? "An unexpected error occurred";
 
            var statusCode = error switch
            {
                ShopCategoryErrors.NotFound or
                ShopCategoryErrors.ShopNotFound => StatusCodes.Status404NotFound,

                ShopCategoryErrors.SlugAlreadyExists or
                ShopCategoryErrors.HasChildren or
                ShopCategoryErrors.HasProducts => StatusCodes.Status409Conflict,

                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(statusCode, Build<T>(false, statusCode, message, default, new List<string> { message }));
        }

        private static ApiResponse<T> Build<T>(bool isSuccess, int statusCode, string message, T? data, List<string>? errors = null) => new()
        {
            IsSuccess = isSuccess,
            StatusCode = statusCode,
            StatusMessage = message,
            Data = data,
            Errors = errors ?? new List<string>()
        };

        #endregion
    }
}