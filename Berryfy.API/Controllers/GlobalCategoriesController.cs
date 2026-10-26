using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Berryfy.Domain.Entities.ProductEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Berryfy.API.Controllers
{
    [Route("api/global-categories")]
    [ApiController]
    public class GlobalCategoriesController : BaseController
    {
        private readonly ICategoryService _categoryService;
        public GlobalCategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }


        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("DefaultPolicy")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryResponse>>>> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();

            if (categories == null)
            {
                return StatusCode(400, new ApiResponse<IEnumerable<CategoryResponse>>
                {
                    IsSuccess = categories.IsSuccess,
                    StatusCode = StatusCodes.Status400BadRequest,
                    StatusMessage = categories.ErrorMessage,
                    Data = categories.Value
                });
            }

            var response = new ApiResponse<IEnumerable<CategoryResponse>>
            {
                IsSuccess = categories.IsSuccess,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = categories.SuccessMessage,
                Data = categories.Value
            };

            return Ok(response);
        }

        [HttpGet]
        [Route("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(int id)
        {

            var category = await _categoryService.GetByIdAsync(id);

            if (!category.IsSuccess)
            {
                return NotFound(new ApiResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Category not found",
                    Data = null
                });
            }

            var response = new ApiResponse<CategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Category retrieved successfully",
                Data = category.Value
            };

            return Ok(response);
        }

        [HttpGet]
        [Route("name/{name}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetByName(string name)
        {
            var category = await _categoryService.GetByNameAsync(name);

            if (category == null)
            {
                return NotFound(new ApiResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Category not found",
                    Data = null
                });
            }

            var response = new ApiResponse<CategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Category retrieved successfully",
                Data = category.Value
            };
            return Ok(response);
        }


        [HttpPost]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create([FromBody] CreateCategoryRequest categoryRequest)
        {
            var createdCategory = await _categoryService.CreateAsync(categoryRequest);

            if (!createdCategory.IsSuccess)
            {
                return new ApiResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error creating category",
                    Errors = new List<string> { "An unexpected error occurred" },
                    Data = null
                };

            }
            var response = new ApiResponse<CategoryResponse>
            {
                IsSuccess = createdCategory.IsSuccess,
                StatusCode = StatusCodes.Status201Created,
                StatusMessage = createdCategory.SuccessMessage,
                Data = createdCategory.Value
            };
            return response;
        }

        [HttpPut]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(int id, [FromBody] UpdateCategoryRequest request)
        {
            var updatedCategory = await _categoryService.UpdateAsync(id, request);

            if (!updatedCategory.IsSuccess)
            {
                return new ApiResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error updating category",
                    Errors = new List<string> { "An unexpected error occurred" },
                    Data = null
                };

            }

            var response = new ApiResponse<CategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Category updated successfully",
                Data = updatedCategory.Value
            };
            return Ok(response);
        }


        [HttpDelete]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var deleted = await _categoryService.DeleteAsync(id);

            if (!deleted.Value)
            {
                var notFoundResponse = new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Category not found",
                    Errors = new List<string> { $"Category with ID {id} not found" },
                    Data = false
                };
                return NotFound(notFoundResponse);
            }

            var response = new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Category deleted successfully",
                Data = true
            };
            return Ok(response);

        }

        [HttpGet]
        [Route("exists-by-id/{id}")]
        //[AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> ExistsById(int id)
        {

            var exists = await _categoryService.ExistsAsync(id);

            if (exists.Value)
            {
                var response = new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Category exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                StatusMessage = "Category not found",
                Data = false
            };
            return NotFound(notFoundResponse);


        }

        [HttpGet]
        [Route("exists-by-name/{name}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> ExistsByName(string name)
        {
            var exists = await _categoryService.ExistsByNameAsync(name);

            if (exists.Value)
            {
                var response = new ApiResponse<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Category exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ApiResponse<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                StatusMessage = "Category not found",
                Data = false
            };
            return NotFound(notFoundResponse);
        }

        [HttpGet("{categoryId}/products")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<Product>>>> GetProductsByCategoryId(int categoryId)
        {
            var products = await _categoryService.GetProductsByCategoryId(categoryId);

            if (!products.IsSuccess)
            {
                return new ApiResponse<List<Product>>()
                {
                    IsSuccess = products.IsSuccess,
                    StatusCode = StatusCodes.Status400BadRequest,
                    StatusMessage = products.ErrorMessage,
                    Data = null
                };
            }

            return new ApiResponse<List<Product>>()
            {
                IsSuccess = products.IsSuccess,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = products.SuccessMessage,
                Data = products.Value
            };
        }

        [HttpGet("{categoryId}/products/count")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<int>>> GetProductCountByCategoryIdAsync(int categoryId)
        {
            int count = _categoryService.GetProductCountByCategoryIdAsync(categoryId).GetAwaiter().GetResult().Value;

            return new ApiResponse<int>()
            {
                IsSuccess = true,
                Data = count
            };
        }
    }
}