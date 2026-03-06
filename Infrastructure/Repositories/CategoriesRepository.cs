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

        public async Task DeleteAsync(CategoryDto category, CancellationToken ct = default)
            => await _db.Delete(category.ToModel());

        public async Task InsertAsync(CategoryDto category, CancellationToken ct = default)
            => await _db.Insert(category.ToModel());

        public Task UpdateAsync(CategoryDto category, CancellationToken ct = default)
            => _db.Update(category.ToModel());
    }
}
