using Domain.Contracts;

namespace Domain.Ports
{
    public interface ICategoriesRepository
    {
        Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default);
        Task<CategoryDto?> GetByIdAsync(Guid categoryId, CancellationToken ct = default);
        Task<CategoryDto> InsertAsync(CategoryDto category, CancellationToken ct = default);
        Task UpdateAsync(CategoryDto category, CancellationToken ct = default);
        Task DeleteAsync(CategoryDto category, CancellationToken ct = default);
    }
}
