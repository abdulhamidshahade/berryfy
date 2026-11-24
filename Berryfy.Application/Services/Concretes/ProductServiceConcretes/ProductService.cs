using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Dtos.ProductDtos.Requests;
using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Application.Dtos.ShopDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.ProductServiceConcretes
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository productRepository,
                              ICategoryRepository categoryRepository,
                              IUnitOfWork unitOfWork,
                              ILogger<ProductService> logger
        )
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task PopulateCategoriesAsync(IEnumerable<ProductResponse> responses)
        {
            var list = responses.Where(r => r != null).ToList();
            if (list.Count == 0) return;

            var allCategoriesRes = await _categoryRepository.GetAllAsync();
            if (!allCategoriesRes.IsSuccess || allCategoriesRes.Value == null) return;

            var categoryLookup = allCategoriesRes.Value.ToDictionary(c => c.Id, CategoryResponse.MapFromCategory);
            foreach (var prod in list)
            {
                prod.ProductCategories ??= new List<CategoryResponse>();
                prod.ProductCategories.Clear();
                if (prod.CategoryId > 0 && categoryLookup.TryGetValue(prod.CategoryId, out var cat))
                {
                    prod.ProductCategories.Add(cat);
                }
            }
        }

        public async Task<ApplicationResponse<IReadOnlyList<ProductResponse>>> GetAllAsync()
        {
            var products = (await _productRepository.GetAllAsync()).Value;
            var mapped = ProductResponse.MapFromProduct(products);
            await PopulateCategoriesAsync(mapped);

            return new ApplicationResponse<IReadOnlyList<ProductResponse>>
            {
                IsSuccess = true,
                Value = mapped
            };
        }

        public async Task<ApplicationResponse<ProductResponse>> GetByIdAsync(int id)
        {
            var product = (await _productRepository.GetByIdAsync(id)).Value;

            if (product == null || product.Id <= 0)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product not found"
                };
            }

            var mapped = ProductResponse.MapFromProduct(product);
            await PopulateCategoriesAsync(new[] { mapped });

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = mapped
            };
        }

        public async Task<ApplicationResponse<ProductResponse>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid product name"
                };
            }

            var product = (await _productRepository.GetByNameAsync(name)).Value;

            if (product == null || product.Id <= 0)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product not found"
                };
            }

            var mapped = ProductResponse.MapFromProduct(product);
            await PopulateCategoriesAsync(new[] { mapped });

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = mapped
            };
        }

        public async Task<ApplicationResponse<ProductResponse>> CreateAsync(CreateProduct productDto)
        {
            if (productDto == null || productDto.Price < 0 || productDto.StockQuantity < 0 ||
                productDto.ReservedStock < 0 || productDto.LowStockThreshold < 0)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid product data"
                };
            }

            if ((await ExistsByNameAsync(productDto.Name)).Value)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product with the same name already exists"
                };
            }

            var product = CreateProduct.MapToProduct(productDto);
            var createdProduct = await _productRepository.CreateAsync(product);

            if (!createdProduct.IsSuccess || createdProduct.Value == null)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to create product"
                };
            }

            var mapped = ProductResponse.MapFromProduct(createdProduct.Value);
            await PopulateCategoriesAsync(new[] { mapped });

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = mapped
            };
        }

        public async Task<ApplicationResponse<ProductResponse>> UpdateAsync(int id, UpdateProduct productDto)
        {
            if (productDto == null || productDto.Price < 0 || productDto.LowStockThreshold < 0)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid product data"
                };
            }

            var existingProduct = (await _productRepository.GetByIdAsync(id)).Value;

            if (existingProduct == null || existingProduct.Id <= 0)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product not found"
                };
            }

            var isProductExists = (await _productRepository.GetByNameAsync(productDto.Name)).Value;

            if (isProductExists != null && isProductExists.Id > 0 && isProductExists.Id != id)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product with the same name already exists"
                };
            }

            var mappedProduct = UpdateProduct.MapToProduct(productDto);
            // Inventory changes belong to the inventory workflow, which logs and validates them.
            mappedProduct.StockQuantity = existingProduct.StockQuantity;
            mappedProduct.ReservedStock = existingProduct.ReservedStock;
            if (mappedProduct.CategoryId <= 0)
            {
                mappedProduct.CategoryId = existingProduct.CategoryId;
            }

            var updatedProduct = await _productRepository.UpdateAsync(id, mappedProduct);

            if (!updatedProduct.IsSuccess || updatedProduct.Value == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to update product"
                };
            }

            var mapped = ProductResponse.MapFromProduct(updatedProduct.Value);
            await PopulateCategoriesAsync(new[] { mapped });

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = mapped
            };
        }

        public async Task<ApplicationResponse<bool>> DeleteAsync(int id)
        {
            var product = _productRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
            if (product == null)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product not found"
                };
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = _productRepository.DeleteAsync(product).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<bool>> ExistsByIdAsync(int id)
        {
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = _productRepository.ExistsByIdAsync(id).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<bool>> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Name is required"
                };
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = _productRepository.ExistsByNameAsync(name).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<PaginationResponse<ProductResponse>>> GetPaginatedAsync(ProductFilter filter)
        {
            _logger.LogInformation("Getting paginated products with filter: {MaxPrice}", filter.MaxPrice);
            var products = _productRepository.GetFilteredAsync(
                filter.SearchTerm,
                filter.Category,
                filter.SortBy,
                filter.MinPrice,
                filter.MaxPrice,
                filter.IsActive,
                filter.PageNumber,
                filter.PageSize
            ).GetAwaiter().GetResult().Value;

            var totalCount = _productRepository.GetFilteredCountAsync(
                filter.SearchTerm,
                filter.Category,
                filter.MinPrice,
                filter.MaxPrice,
                filter.IsActive
            ).GetAwaiter().GetResult().Value;

            var productDtos = ProductResponse.MapFromProduct(products);
            await PopulateCategoriesAsync(productDtos);
            var paginationResult = new PaginationResponse<ProductResponse>(
                productDtos,
                filter.PageNumber,
                filter.PageSize,
                totalCount
            );

            return new ApplicationResponse<PaginationResponse<ProductResponse>>
            {
                IsSuccess = true,
                Value = paginationResult
            };
        }

        //public async Task<ApplicationResponse<ShopCategoryResponse>> GetShopCategoryByProductAsync(int productId)
        //{
        //    var shopCategory = await _productRepository.GetShopCategoryByProduct(productId);

        //    if (!shopCategory.IsSuccess)
        //    {
        //        return new ApplicationResponse<ShopCategoryResponse> { IsSuccess = shopCategory.IsSuccess, ErrorMessage = shopCategory.Message };
        //    }

        //    return new ApplicationResponse<ShopCategoryResponse>()
        //    {
        //        IsSuccess = shopCategory.IsSuccess,
        //        SuccessMessage = shopCategory.Message,
        //        //Value = ShopCategoryResponse.MapFromShopCategory(shopCategory.Value)
        //    };
        //}
    }
}