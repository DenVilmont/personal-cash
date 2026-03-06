using Domain.Contracts;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public sealed class UserSettingsRepository(DatabaseService db) : IUserSettingsRepository
    {
        private readonly DatabaseService _db = db;

        public async Task<UserSettingsDto?> GetAsync(CancellationToken ct = default)
        {
            var list = await _db.From<UserSettings>();
            return list.FirstOrDefault()?.ToDto();
        }

        public async Task<UserSettingsDto> InsertReturningAsync(UserSettingsDto settings, CancellationToken ct = default)
        {
            var inserted = (await _db.Insert(settings.ToModel())).FirstOrDefault()
                ?? throw new InvalidOperationException("Insert user_settings returned no row.");

            return inserted.ToDto();
        }

        public async Task<UserSettingsDto> UpdateReturningAsync(UserSettingsDto settings, CancellationToken ct = default)
        {
            var updated = (await _db.Update(settings.ToModel())).FirstOrDefault()
                ?? throw new InvalidOperationException("Update user_settings returned no row.");

            return updated.ToDto();
        }
    }
}
