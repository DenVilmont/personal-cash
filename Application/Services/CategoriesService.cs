using Application.Common;
using Domain.Contracts;
using Domain.Ports;

namespace Application.Services
{
    public class CategoriesService(ICategoriesRepository categoriesRepo, ITransactionsLookup txLookup)
    {
        private const string DefaultTransferCategoryName = "Transfer";

        private readonly ICategoriesRepository _categoriesRepo = categoriesRepo;
        private readonly ITransactionsLookup _txLookup = txLookup;

        public async Task<List<CategoryDto>> GetSortedAsync()
            => (await _categoriesRepo.ListAsync())
            .OrderBy(x => x.Name)
            .ToList();

        public async Task<CategoryDto> EnsureTransferCategoryAsync(Guid userId)
        {
            var categories = await _categoriesRepo.ListAsync();

            var transferCategories = categories
                .Where(c => c.IsTransferCategory)
                .ToList();

            if (transferCategories.Count > 1)
            {
                throw new AppValidationException(
                    "Multiple transfer categories found. Data is inconsistent.");
            }

            if (transferCategories.Count == 1)
                return transferCategories[0];

            var item = new CategoryDto
            {
                UserId = userId,
                Name = DefaultTransferCategoryName,
                IsTransferCategory = true
            };

            return await _categoriesRepo.InsertAsync(item);
        }

        public async Task AddAsync(Guid userId, string name)
        {
            var normalizedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new AppValidationException("Enter category name");

            var existing = await _categoriesRepo.ListAsync();
            if (existing.Any(c => string.Equals(
                    c.Name,
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                throw new AppValidationException("Category already exists");
            }

            var item = new CategoryDto
            {
                UserId = userId,
                Name = normalizedName,
            };

            await _categoriesRepo.InsertAsync(item);
        }

        public async Task UpdateAsync(CategoryDto category)
        {
            var normalizedName = (category.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new AppValidationException("Enter category name");

            var existing = await _categoriesRepo.ListAsync();

            var storedCategory = existing.SingleOrDefault(c => c.Id == category.Id);
            if (storedCategory is null)
                throw new AppValidationException("Category not found");

            if (existing.Any(c =>
                    c.Id != category.Id &&
                    string.Equals(
                        c.Name,
                        normalizedName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                throw new AppValidationException("Category already exists");
            }

            var updatedCategory = new CategoryDto
            {
                Id = storedCategory.Id,
                UserId = storedCategory.UserId,
                Name = normalizedName,
                IsTransferCategory = storedCategory.IsTransferCategory,
                CreatedAt = storedCategory.CreatedAt
            };

            await _categoriesRepo.UpdateAsync(updatedCategory);
        }

        public async Task DeleteAsync(CategoryDto category)
        {
            if (category.IsTransferCategory)
            {
                throw new AppValidationException(
                    "Transfer category can't be deleted.");
            }

            if (await _txLookup.AnyForCategoryAsync(category.Id))
            {
                throw new AppValidationException(
                    "Category has transactions. It can't be deleted.");
            }

            await _categoriesRepo.DeleteAsync(category);
        }
    }
}
