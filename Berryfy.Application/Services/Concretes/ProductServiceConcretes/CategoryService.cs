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

        public async Task<CategoryResponse> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                return null;
            }

            return CategoryResponse.MapFromCategory(category);
        }

        public async Task<CategoryResponse> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var category = await _categoryRepository.GetByNameAsync(name);
            
            if (category == null)
            {
                return null;
            }

            return CategoryResponse.MapFromCategory(category);
        }

        public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Select(CategoryResponse.MapFromCategory);
        }

        public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest categoryRequest)
        {
            if (categoryRequest == null)
            {
                return null;
            }

            if (await ExistsByNameAsync(categoryRequest.Name))
            {
                return null;
            }

            var category = CreateCategoryRequest.MapToCategory(categoryRequest);

            var createdCategory = await _categoryRepository.CreateAsync(category);
            return CategoryResponse.MapFromCategory(createdCategory);
        }


        public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var existingCategory = await _categoryRepository.GetByIdAsync(id);

            if (existingCategory == null)
            {
                return null;
            }

            var isNameExists = await GetByNameAsync(request.Name);

            if(isNameExists != null && isNameExists.Id != id)
            {
                return null;
            }

            var mappedCategory = UpdateCategoryRequest.MapToCategory(request);

            var updatedCategory = await _categoryRepository.UpdateAsync(id, mappedCategory);


            return CategoryResponse.MapFromCategory(updatedCategory);
        }

        

        public async Task<bool> DeleteAsync(int id)
        {
            if(id <= 0)
            {
                return false;
            }

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return false;
            }

            var deletedCategory = await _categoryRepository.DeleteAsync(category);


            return deletedCategory;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            if(id <= 0)
            {
                return false;
            }

            return await _categoryRepository.ExistsByIdAsync(id);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return await _categoryRepository.ExistsByNameAsync(name);
        }
    }
}
