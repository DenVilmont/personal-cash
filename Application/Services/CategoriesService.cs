using Application.Common;
using Domain.Contracts;
using Domain.Ports;

namespace Application.Services
{
    public class CategoriesService(ICategoriesRepository categoriesRepo, ITransactionsLookup txLookup)
    {
        private readonly ICategoriesRepository _categoriesRepo = categoriesRepo;
        private readonly ITransactionsLookup _txLookup = txLookup;

        public async Task<List<CategoryDto>> GetSortedAsync()
            => (await _categoriesRepo.ListAsync())
            .OrderBy(x => x.Name)
            .ToList();

        public async Task AddAsync(Guid userId, string name)
        {
            var normalizedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new AppValidationException("Enter category name");

            var existing = await _categoriesRepo.ListAsync();
            if (existing.Any(c => string.Equals(c.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
                throw new AppValidationException("Category already exists");

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
            if (existing.Any(c => c.Id != category.Id && string.Equals(c.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
                throw new AppValidationException("Category already exists");

            category.Name = normalizedName;
            await _categoriesRepo.UpdateAsync(category);
        }

        public async Task DeleteAsync(CategoryDto category)
        {
            if (await _txLookup.AnyForCategoryAsync(category.Id))
                throw new AppValidationException("Category has transactions. It can't be deleted.");

            await _categoriesRepo.DeleteAsync(category);
        }

    }
}
