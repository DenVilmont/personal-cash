using Domain.Contracts;

namespace Domain.Ports
{
    public interface IUserPageStatesRepository
    {
        Task<UserPageStateDto?> GetByPageKeyAsync(string pageKey, CancellationToken ct = default);
        Task<UserPageStateDto> InsertReturningAsync(UserPageStateDto state, CancellationToken ct = default);
        Task<UserPageStateDto> UpdateReturningAsync(UserPageStateDto state, CancellationToken ct = default);
        Task DeleteByPageKeyAsync(string pageKey, CancellationToken ct = default);
    }
}
