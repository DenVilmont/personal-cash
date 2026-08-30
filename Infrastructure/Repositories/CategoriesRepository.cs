using Domain.Contracts;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public sealed class CategoriesRepository(DatabaseService db) : ICategoriesRepository
    {
        private readonly DatabaseService _db = db;

        public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default)
            => (await _db.From<Category>())
            .Select(x => x.ToDto())
            .ToList();

        public async Task<CategoryDto?> GetByIdAsync(Guid categoryId, CancellationToken ct = default)
        {
            var model = await _db.Single<Category>(
                q => q.Eq("id", categoryId));

            return model?.ToDto();
        }

        public async Task DeleteAsync(CategoryDto category, CancellationToken ct = default)
            => await _db.Delete(category.ToModel());

        public async Task<CategoryDto> InsertAsync(CategoryDto category, CancellationToken ct = default)
            => (await _db.Insert(category.ToModel()))
            .Single()
            .ToDto();

        public Task UpdateAsync(CategoryDto category, CancellationToken ct = default)
            => _db.Update(category.ToModel());
    }
}
