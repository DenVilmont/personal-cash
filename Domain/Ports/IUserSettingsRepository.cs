using Domain.Contracts;

namespace Domain.Ports
{
    public interface IUserSettingsRepository
    {
        Task<UserSettingsDto?> GetAsync(CancellationToken ct = default);
        Task<UserSettingsDto> InsertReturningAsync(UserSettingsDto settings, CancellationToken ct = default);
        Task<UserSettingsDto> UpdateReturningAsync(UserSettingsDto settings, CancellationToken ct = default);
    }
}
