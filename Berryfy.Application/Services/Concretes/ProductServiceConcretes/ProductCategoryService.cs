
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

        public async Task<bool> AddProductCategoryAsync(ProductResponse product, List<int> categories)
        {
            if(categories.Count == 0)
            {
                return false;
            }

            var mappedProduct = ProductResponse.MapToProduct(product);

            var created = await _productCategoryRepository.AddProductCategoryAsync(mappedProduct, categories);

            return created;
        }

        public async Task<bool> UpdateProductCategoryAsync(ProductResponse product, List<int> categories)
        {
            if (categories.Count == 0 || !await _productRepository.ExistsByIdAsync(product.Id))
            {
                return false;
            }

            //await _unitOfWork.BeginTransactionAsync();

            var existingCategories = await _productCategoryRepository.GetCategoriesByProuductId(product.Id);
            
            if(!await _productCategoryRepository.RemoveCategoriesByProductId(product.Id))
            {
                //await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            //await _unitOfWork.CommitTransactionAsync();

            var mappedProduct = ProductResponse.MapToProduct(product);

            bool result = await _productCategoryRepository.AddProductCategoryAsync(mappedProduct, categories);

            return result;
        }
    }
}
