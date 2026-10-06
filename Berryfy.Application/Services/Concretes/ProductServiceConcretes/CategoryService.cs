using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.CategoryDtos.Requests;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
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

        public async Task<ApplicationResponse<CategoryResponse>> GetByIdAsync(int id)
        {
            var category = _categoryRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;

            if (category == null)
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                };
            }

            return new ApplicationResponse<CategoryResponse>
            {
                IsSuccess = true,
                Value = CategoryResponse.MapFromCategory(category)
            };
        }

        public async Task<ApplicationResponse<CategoryResponse>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category name"
                };
            }

            var category = _categoryRepository.GetByNameAsync(name).GetAwaiter().GetResult().Value;
            
            if (category == null)
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                };
            }

            return new ApplicationResponse<CategoryResponse>
            {
                IsSuccess = true,
                Value = CategoryResponse.MapFromCategory(category)
            };
        }

        public async Task<ApplicationResponse<IEnumerable<CategoryResponse>>> GetAllAsync()
        {
            var categories = _categoryRepository.GetAllAsync().GetAwaiter().GetResult().Value;
            return new ApplicationResponse<IEnumerable<CategoryResponse>>
            {
                IsSuccess = true,
                Value = categories.Select(CategoryResponse.MapFromCategory)
            };
        }

        public async Task<ApplicationResponse<CategoryResponse>> CreateAsync(CreateCategoryRequest categoryRequest)
        {
            if (categoryRequest == null)
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category request"
                };
            }

            if (ExistsByNameAsync(categoryRequest.Name).GetAwaiter().GetResult().Value)
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category already exists"
                };
            }

            var category = CreateCategoryRequest.MapToCategory(categoryRequest);

            var createdCategory = _categoryRepository.CreateAsync(category).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<CategoryResponse>
            {
                IsSuccess = true,
                Value = CategoryResponse.MapFromCategory(createdCategory)
            };
        }


        public async Task<ApplicationResponse<CategoryResponse>> UpdateAsync(int id, UpdateCategoryRequest request)
        {
            if (request == null)
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid category request"
                };
            }

            var existingCategory = _categoryRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;

            if (existingCategory == null)
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                };
            }

            var isNameExists = GetByNameAsync(request.Name).GetAwaiter().GetResult().Value;

            if(isNameExists != null && isNameExists.Id != id)
            {
                return new ApplicationResponse<CategoryResponse>
                {
                    IsSuccess = false,
                    ErrorMessage = "Category already exists"
                };
            }

            var mappedCategory = UpdateCategoryRequest.MapToCategory(request);

            var updatedCategory = _categoryRepository.UpdateAsync(id, mappedCategory).GetAwaiter().GetResult().Value;


            return new ApplicationResponse<CategoryResponse>
            {
                IsSuccess = true,
                Value = CategoryResponse.MapFromCategory(updatedCategory)
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
    }
}