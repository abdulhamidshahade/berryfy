using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ProductDtos.Requests;
using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Berryfy.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    [Tags("Products")]
    public class ProductsController : BaseController
    {
        private readonly IProductService _productService;


        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }


        [HttpGet]
        [AllowAnonymous]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EndpointName("GetAllProducts")]
        [EndpointSummary("Fetches all products")]
        [EndpointDescription("Returns a paginated list of products. Supports page number and page size parameters.")]
        [EnableRateLimiting("DefaultPolicy")]
        public async Task<ActionResult<ResponseDto<PaginationDto<ProductResponse>>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? category = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? isActive = true)
        {

            var filter = new ProductFilter
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Category = category,
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                IsActive = isActive
            };

            var result = await _productService.GetPaginatedAsync(filter);

            if (!result.Data.Any())
            {
                return NotFound(new ResponseDto<PaginationDto<ProductResponse>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "No products found",
                    Data = result
                });
            }

            return Ok(new ResponseDto<PaginationDto<ProductResponse>>
            {
                Data = result,
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Products retrieved successfully!"
            });
        }


        [HttpGet]
        [Route("all")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<IEnumerable<ProductResponse>>>> GetProducts()
        {

            var products = await _productService.GetAllAsync();

            if (!products.Any())
            {
                return NotFound(new ResponseDto<IEnumerable<ProductResponse>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "No products found",
                });
            }
            var response = new ResponseDto<IEnumerable<ProductResponse>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Products retrieved successfully",
                Data = products
            };

            return Ok(response);
        }


        [HttpGet]
        [Route("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<ProductResponse>>> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
            {
                return new ResponseDto<ProductResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error retrieving product",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ResponseDto<ProductResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Product retrieved successfully",
                Data = product
            };
            return Ok(response);
        }


        [HttpGet]
        [Route("name/{name}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<ProductResponse>>> GetByName(string name)
        {
            var product = await _productService.GetByNameAsync(name);

            if (product == null)
            {
                return new ResponseDto<ProductResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error retrieving product",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ResponseDto<ProductResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Product retrieved successfully",
                Data = product
            };
            return Ok(response);
        }


        [HttpPost]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<ProductResponse>>> Create([FromBody] CreateProduct productDto,
            [FromQuery] List<int> categories)
        {
            var createdProduct = await _productService.CreateAsync(productDto, categories);

            if (createdProduct == null)
            {
                return new ResponseDto<ProductResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error creating product",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }

            var response = new ResponseDto<ProductResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                StatusMessage = "Product created successfully",
                Data = createdProduct
            };

            return response;
        }



        [HttpPut]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<ProductResponse>>> Update(int id, [FromBody] UpdateProduct productDto,
            [FromQuery] List<int> categories)
        {
            var updatedProduct = await _productService.UpdateAsync(id, productDto, categories);

            if (updatedProduct == null)
            {
                return new ResponseDto<ProductResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error updating product",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }

            var response = new ResponseDto<ProductResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Product updated successfully",
                Data = updatedProduct
            };
            return Ok(response);
        }


        [HttpDelete]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> Delete(int id)
        {
            var deleted = await _productService.DeleteAsync(id);

            if (!deleted)
            {
                var notFoundResponse = new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = "Product not found",
                    Errors = new List<string> { $"Product with ID {id} not found" },
                    Data = false
                };
                return NotFound(notFoundResponse);
            }

            var response = new ResponseDto<bool>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Product deleted successfully",
                Data = true
            };
            return Ok(response);
        }


        [HttpGet]
        [Route("exists-by-id/{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> ExistsById(int id)
        {

            var exists = await _productService.ExistsByIdAsync(id);

            if (exists)
            {
                var response = new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Product exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ResponseDto<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                StatusMessage = "Product not found",
                Data = false
            };
            return NotFound(notFoundResponse);
        }


        [HttpGet]
        [Route("exists-by-name/{name}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> ExistsByName(string name)
        {
            var exists = await _productService.ExistsByNameAsync(name);

            if (exists)
            {
                var response = new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    StatusMessage = "Product exists",
                    Data = true
                };
                return Ok(response);
            }

            var notFoundResponse = new ResponseDto<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                StatusMessage = "Product not found",
                Data = false
            };

            return NotFound(notFoundResponse);
        }
    }
}
