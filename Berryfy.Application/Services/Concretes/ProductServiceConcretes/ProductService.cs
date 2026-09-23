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

        
        public async Task<IReadOnlyList<ProductResponse>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return ProductResponse.MapFromProduct(products);
        }

        public async Task<ProductResponse> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return null;
            }

            return ProductResponse.MapFromProduct(product);
        }

        public async Task<ProductResponse> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var product = await _productRepository.GetByNameAsync(name);

            if (product == null)
            {
                return null;
            }

            return ProductResponse.MapFromProduct(product);
        }


        public async Task<ProductResponse> CreateAsync(CreateProduct productDto, List<int> categories)
        {
            if (productDto == null || productDto.Price < 0 || productDto.StockQuantity < 0 ||
                productDto.ReservedStock != 0 || productDto.LowStockThreshold < 0)
            {
                return null;
            }

            if (await ExistsByNameAsync(productDto.Name))
            {
                return null;
            }

            await _unitOfWork.BeginTransactionAsync();

            var product = CreateProduct.MapToProduct(productDto);
            var createdProduct = await _productRepository.CreateAsync(product);

            var productToDto = ProductResponse.MapFromProduct(createdProduct);

            if (!await _productCategoryService.AddProductCategoryAsync(productToDto, categories))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return null;
            }

            await _unitOfWork.CommitTransactionAsync();

            return ProductResponse.MapFromProduct(await _productRepository.GetByIdAsync(createdProduct.Id));
        }

        
        public async Task<ProductResponse> UpdateAsync(int id, UpdateProduct productDto, List<int> categories)
        {
            if (productDto == null || productDto.Price < 0 || productDto.LowStockThreshold < 0)
            {
                return null;
            }

            var existingProduct = await _productRepository.GetByIdAsync(id);

            if (existingProduct == null)
            {
                return null;
            }

            var isProductExists = await GetByNameAsync(productDto.Name);

            if (isProductExists != null && isProductExists.Id != id)
            {
                return null;
            }

            var mappedProduct = UpdateProduct.MapToProduct(productDto);
            // Inventory changes belong to the inventory workflow, which logs and validates them.
            mappedProduct.StockQuantity = existingProduct.StockQuantity;
            mappedProduct.ReservedStock = existingProduct.ReservedStock;

            await _unitOfWork.BeginTransactionAsync();

            var updatedProduct = await _productRepository.UpdateAsync(id, mappedProduct);

            var productToDto = ProductResponse.MapFromProduct(updatedProduct);

            if (!await _productCategoryService.UpdateProductCategoryAsync(productToDto, categories))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return null;
            }

            await _unitOfWork.CommitTransactionAsync();

            var returnProduct = await _productRepository.GetByIdAsync(updatedProduct.Id);

            return ProductResponse.MapFromProduct(returnProduct);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return false;
            }

            return await _productRepository.DeleteAsync(product);
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            return await _productRepository.ExistsByIdAsync(id);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return await _productRepository.ExistsByNameAsync(name);
        }

        public async Task<PaginationDto<ProductResponse>> GetPaginatedAsync(ProductFilter filter)
        {
            _logger.LogInformation("Getting paginated products with filter: {MaxPrice}", filter.MaxPrice);
            var products = await _productRepository.GetFilteredAsync(
                filter.SearchTerm,
                filter.Category,
                filter.SortBy,
                filter.MinPrice,
                filter.MaxPrice,
                filter.IsActive,
                filter.PageNumber,
                filter.PageSize
            );

            var totalCount = await _productRepository.GetFilteredCountAsync(
                filter.SearchTerm,
                filter.Category,
                filter.MinPrice,
                filter.MaxPrice,
                filter.IsActive
            );

            var productDtos = ProductResponse.MapFromProduct(products);
            var paginationResult = new PaginationDto<ProductResponse>(
                productDtos,
                filter.PageNumber,
                filter.PageSize,
                totalCount
            );

            return paginationResult;
        }
    }
}
