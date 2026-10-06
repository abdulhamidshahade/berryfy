
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.ProductInterfaces;


namespace Berryfy.Application.Services.Concretes.ProductServiceConcretes
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        public ProductCategoryService(IProductCategoryRepository productCategoryRepository, 
            IUnitOfWork unitOfWork,
            IProductRepository productRepository)
        {
            _productCategoryRepository = productCategoryRepository;
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
        }

        public async Task<ApplicationResponse<bool>> AddProductCategoryAsync(ProductResponse product, List<int> categories)
        {
            if(categories.Count == 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "No categories provided"
                };
            }

            var mappedProduct = ProductResponse.MapToProduct(product);

            var created = _productCategoryRepository.AddProductCategoryAsync(mappedProduct, categories).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = created
            };
        }

        public async Task<ApplicationResponse<bool>> UpdateProductCategoryAsync(ProductResponse product, List<int> categories)
        {
            if (categories.Count == 0 || !_productRepository.ExistsByIdAsync(product.Id).GetAwaiter().GetResult().Value)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid product or no categories provided"
                };
            }

            //await _unitOfWork.BeginTransactionAsync();

            var existingCategories = await _productCategoryRepository.GetCategoriesByProuductId(product.Id);
            
            if(!_productCategoryRepository.RemoveCategoriesByProductId(product.Id).GetAwaiter().GetResult().Value)
            {
                //await _unitOfWork.RollbackTransactionAsync();
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to update product categories"
                };
            }

            //await _unitOfWork.CommitTransactionAsync();

            var mappedProduct = ProductResponse.MapToProduct(product);

            bool result =_productCategoryRepository.AddProductCategoryAsync(mappedProduct, categories).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = result
            };
        }
    }
}