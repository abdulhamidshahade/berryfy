using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Berryfy.API.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }


        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("DefaultPolicy")]
        public async Task<ActionResult<ResponseDto<IEnumerable<CategoryResponse>>>> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();

            if (categories == null)
            {
                return StatusCode(400, new ResponseDto<IEnumerable<CategoryResponse>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    StatusMessage = "Failed retriving categories",
                    Data = null
                });
            }

            var response = new ResponseDto<IEnumerable<CategoryResponse>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Categories retrieved successfully",
                Data = categories
            };

            return Ok(response);
        }


        [HttpGet]
        [Route("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<CategoryResponse>>> GetById(int id)
        {

            var category = await _categoryService.GetByIdAsync(id);

            if (category == null)
            {
                return NotFound(new ResponseDto<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Category not found",
                    Data = null
                });
            }

            var response = new ResponseDto<CategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Category retrieved successfully",
                Data = category
            };

            return Ok(response);
        }


        [HttpGet]
        [Route("name/{name}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<CategoryResponse>>> GetByName(string name)
        {
            var category = await _categoryService.GetByNameAsync(name);

            if (category == null)
            {
                return NotFound(new ResponseDto<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Category not found",
                    Data = null
                });
            }

            var response = new ResponseDto<CategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Category retrieved successfully",
                Data = category
            };
            return Ok(response);
        }


        [HttpPost]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<CategoryResponse>>> Create([FromBody] CreateCategoryRequest categoryRequest)
        {
            var createdCategory = await _categoryService.CreateAsync(categoryRequest);

            if (createdCategory == null)
            {
                return new ResponseDto<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error creating category",
                    Errors = new List<string> { "An unexpected error occurred" },
                    Data = null
                };

            }
            var response = new ResponseDto<CategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                StatusMessage = "Category created successfully",
                Data = createdCategory
            };
            return response;
        }

        [HttpPut]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<CategoryResponse>>> Update(int id, [FromBody] UpdateCategoryRequest request)
        {
            var updatedCategory = await _categoryService.UpdateAsync(id, request);

            if (updatedCategory == null)
            {
                return new ResponseDto<CategoryResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error updating category",
                    Errors = new List<string> { "An unexpected error occurred" },
                    Data = null
                };

            }

            var response = new ResponseDto<CategoryResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Category updated successfully",
                Data = updatedCategory
            };
            return Ok(response);
        }


        [HttpDelete]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> Delete(int id)
        {
            var deleted = await _categoryService.DeleteAsync(id);

            if (!deleted)
            {
                var notFoundResponse = new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Category not found",
                    Errors = new List<string> { $"Category with ID {id} not found" },
                    Data = false
                };
                return NotFound(notFoundResponse);
            }

            var response = new ResponseDto<bool>
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
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> ExistsById(int id)
        {

            var exists = await _categoryService.ExistsAsync(id);

            if (exists)
            {
                var response = new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Category exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ResponseDto<bool>
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
        public async Task<ActionResult<ResponseDto<bool>>> ExistsByName(string name)
        {
            var exists = await _categoryService.ExistsByNameAsync(name);

            if (exists)
            {
                var response = new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Category exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ResponseDto<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                StatusMessage = "Category not found",
                Data = false
            };
            return NotFound(notFoundResponse);
        }
    }
}