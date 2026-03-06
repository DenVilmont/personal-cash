using Domain.Contracts;
using Infrastructure.Models;

namespace Infrastructure.Mapping
{
    public static class UserSettingsMapping
    {
        public static UserSettingsDto ToDto(this UserSettings m) => new()
        {
            UserId = m.UserId,
            AvatarBase64 = m.AvatarBase64,
            AvatarMime = m.AvatarMime,
            FirstName = m.FirstName,
            LastName = m.LastName,
            PreferredLanguage = m.PreferredLanguage,
            PreferredCurrency = m.PreferredCurrency,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };

        public static UserSettings ToModel(this UserSettingsDto d)
        {
            var m = new UserSettings
            {
                UserId = d.UserId,
                AvatarBase64 = d.AvatarBase64,
                AvatarMime = d.AvatarMime,
                FirstName = d.FirstName,
                LastName = d.LastName,
                PreferredLanguage = d.PreferredLanguage,
                PreferredCurrency = d.PreferredCurrency,
                UpdatedAt = d.UpdatedAt == default ? DateTimeOffset.UtcNow : d.UpdatedAt
            };

            if (d.CreatedAt != default)
                m.CreatedAt = d.CreatedAt;

            return m;
        }
    }
}
