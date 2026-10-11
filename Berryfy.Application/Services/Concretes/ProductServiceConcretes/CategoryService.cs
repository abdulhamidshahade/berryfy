using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories.ProductInterfaces;

namespace Berryfy.Application.Services.Concretes.ProductServiceConcretes
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<ApplicationResponse<GlobalCategoryResponse>> GetByIdAsync(int id)
        {
            var category = _categoryRepository.GetByIdAsync(id).GetAwaiter().GetResult();

            if (!category.IsSuccess)
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                };
            }

            return new ApplicationResponse<GlobalCategoryResponse>
            {
                IsSuccess = true,
                Value = GlobalCategoryResponse.MapFromCategory(category.Value)
            };
        }

        public async Task<ApplicationResponse<GlobalCategoryResponse>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category name"
                };
            }

            var category = _categoryRepository.GetByNameAsync(name).GetAwaiter().GetResult().Value;
            
            if (category == null)
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                };
            }

            return new ApplicationResponse<GlobalCategoryResponse>
            {
                IsSuccess = true,
                Value = GlobalCategoryResponse.MapFromCategory(category)
            };
        }

        public async Task<ApplicationResponse<IEnumerable<GlobalCategoryResponse>>> GetAllAsync()
        {
            var categories = _categoryRepository.GetAllAsync().GetAwaiter().GetResult().Value;
            return new ApplicationResponse<IEnumerable<GlobalCategoryResponse>>
            {
                IsSuccess = true,
                Value = categories.Select(GlobalCategoryResponse.MapFromCategory)
            };
        }

        public async Task<ApplicationResponse<GlobalCategoryResponse>> CreateAsync(CreateCategoryRequest categoryRequest)
        {
            if (categoryRequest == null)
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category request"
                };
            }

            if (ExistsByNameAsync(categoryRequest.Name).GetAwaiter().GetResult().Value)
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category already exists"
                };
            }

            var category = CreateCategoryRequest.MapToCategory(categoryRequest);

            var createdCategory = _categoryRepository.CreateAsync(category).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<GlobalCategoryResponse>
            {
                IsSuccess = true,
                Value = GlobalCategoryResponse.MapFromCategory(createdCategory)
            };
        }


        public async Task<ApplicationResponse<GlobalCategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest request)
        {
            if (request == null)
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category request"
                };
            }

            var existingCategory = _categoryRepository.GetByIdAsync(id).GetAwaiter().GetResult();

            if (!existingCategory.IsSuccess)
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                };
            }

            var isNameExists = GetByNameAsync(request.Name).GetAwaiter().GetResult();

            if(isNameExists.IsSuccess && isNameExists.Value.Id != id)
            {
                return new ApplicationResponse<GlobalCategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category already exists"
                };
            }

            var mappedCategory = UpdateCategoryRequest.MapToCategory(request);

            var updatedCategory = _categoryRepository.UpdateAsync(id, mappedCategory).GetAwaiter().GetResult();


            return new ApplicationResponse<GlobalCategoryResponse>
            {
                IsSuccess = true,
                Value = GlobalCategoryResponse.MapFromCategory(updatedCategory.Value)
            };
        }

        

        public async Task<ApplicationResponse<bool>> DeleteAsync(int id)
        {
            if(id <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category ID"
                };
            }

            var category = _categoryRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
            if (category == null)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                };
            }

            var deletedCategory = _categoryRepository.DeleteAsync(category).GetAwaiter().GetResult().Value;


            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = deletedCategory
            };
        }

        public async Task<ApplicationResponse<bool>> ExistsAsync(int id)
        {
            if(id <= 0)
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category ID"
                };
            }

            var exists = _categoryRepository.ExistsByIdAsync(id).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = exists
            };
        }

        public async Task<ApplicationResponse<bool>> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category name"
                };
            }

            var exists = _categoryRepository.ExistsByNameAsync(name).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = exists
            };
        }

        public async Task<ApplicationResponse<List<Product>>> GetProductsByCategoryId(int categoryId)
        {
            var categories = await _categoryRepository.GetProductsByGlobalCategoryId(categoryId);

            if (!categories.IsSuccess)
            {
                return new ApplicationResponse<List<Product>>()
                {
                    IsSuccess = categories.IsSuccess,
                    ErrorMessage = categories.Message,
                    Value = new List<Product>()
                };
            }

            return new ApplicationResponse<List<Product>>()
            {
                IsSuccess = categories.IsSuccess,
                Value = categories.Value,
                SuccessMessage = categories.Message
            };
        }

        public async Task<InfrastructureResponse<int>> GetProductCountByCategoryIdAsync(int categoryId)
        {
            int count = _categoryRepository.GetProductCountByGlobalCategoryIdAsync(categoryId).GetAwaiter().GetResult().Value;

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Value = count
            };
        }
    }
}