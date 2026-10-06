using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ProductDtos.Requests;
using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.ProductServiceConcretes
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository productRepository,
                              IProductCategoryService productCategoryService,
                              IUnitOfWork unitOfWork,
                              ILogger<ProductService> logger
        )
        {
            _productRepository = productRepository;
            _productCategoryService = productCategoryService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        
        public async Task<ApplicationResponse<IReadOnlyList<ProductResponse>>> GetAllAsync()
        {
            var products = _productRepository.GetAllAsync().GetAwaiter().GetResult().Value;

            return new ApplicationResponse<IReadOnlyList<ProductResponse>>
            {
                IsSuccess = true,
                Value = ProductResponse.MapFromProduct(products)
            };
        }

        public async Task<ApplicationResponse<ProductResponse>> GetByIdAsync(int id)
        {
            var product = _productRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;

            if (product == null)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product not found"
                };
            }

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = ProductResponse.MapFromProduct(product)
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

            var product = _productRepository.GetByNameAsync(name).GetAwaiter().GetResult().Value;

            if (product == null)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product not found"
                };
            }

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = ProductResponse.MapFromProduct(product)
            };
        }


        public async Task<ApplicationResponse<ProductResponse>> CreateAsync(CreateProduct productDto, List<int> categories)
        {
            if (productDto == null || productDto.Price < 0 || productDto.StockQuantity < 0 ||
                productDto.ReservedStock != 0 || productDto.LowStockThreshold < 0)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid product data"
                };
            }

            if (ExistsByNameAsync(productDto.Name).GetAwaiter().GetResult().Value)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product with the same name already exists"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            var product = CreateProduct.MapToProduct(productDto);
            var createdProduct = _productRepository.CreateAsync(product).GetAwaiter().GetResult().Value;

            var productToDto = ProductResponse.MapFromProduct(createdProduct);

            if (!_productCategoryService.AddProductCategoryAsync(productToDto, categories).GetAwaiter().GetResult().Value)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to add product categories"
                };
            }

            await _unitOfWork.CommitTransactionAsync();

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = ProductResponse.MapFromProduct(_productRepository.GetByIdAsync(createdProduct.Id).GetAwaiter().GetResult().Value)
            };
        }

        
        public async Task<ApplicationResponse<ProductResponse>> UpdateAsync(int id, UpdateProduct productDto, List<int> categories)
        {
            if (productDto == null || productDto.Price < 0 || productDto.LowStockThreshold < 0)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid product data"
                };
            }

            var existingProduct = _productRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;

            if (existingProduct == null)
            {
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Product not found"
                };
            }

            var isProductExists = _productRepository.GetByNameAsync(productDto.Name).GetAwaiter().GetResult().Value;

            if (isProductExists != null && isProductExists.Id != id)
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

            await _unitOfWork.BeginTransactionAsync();

            var updatedProduct = _productRepository.UpdateAsync(id, mappedProduct).GetAwaiter().GetResult().Value;

            var productToDto = ProductResponse.MapFromProduct(updatedProduct);

            if (!_productCategoryService.UpdateProductCategoryAsync(productToDto, categories).GetAwaiter().GetResult().Value)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApplicationResponse<ProductResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to update product categories"
                };
            }

            await _unitOfWork.CommitTransactionAsync();

            var returnProduct = _productRepository.GetByIdAsync(updatedProduct.Id).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<ProductResponse>
            {
                IsSuccess = true,
                Value = ProductResponse.MapFromProduct(returnProduct)
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
    }
}
