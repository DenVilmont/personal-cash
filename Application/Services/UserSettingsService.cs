using Domain.Contracts;
using Domain.Ports;

namespace Application.Services;

public sealed class UserSettingsService(IUserSettingsRepository repo)
{
    private readonly IUserSettingsRepository _repo = repo;

    public async Task<UserSettingsDto> LoadAsync(Guid userId)
    {
        var existing = await _repo.GetAsync();
        if (existing is not null)
        {
            existing.UserId = userId; // на всякий случай
            return existing;
        }

        return new UserSettingsDto { UserId = userId };
    }

    public async Task<(UserSettingsDto Saved, bool LanguageChanged)> SaveAsync(Guid userId, UserSettingsDto settings)
    {
        var existing = await _repo.GetAsync();
        var oldLanguage = existing?.PreferredLanguage;

        settings.UserId = userId;

        settings.PreferredCurrency = NormalizeCurrency(settings.PreferredCurrency);
        settings.PreferredLanguage = NormalizeLanguage(settings.PreferredLanguage);

        var saved = existing is null
            ? await _repo.InsertReturningAsync(settings)
            : await _repo.UpdateReturningAsync(settings);

        var languageChanged = !string.Equals(oldLanguage, saved.PreferredLanguage, StringComparison.OrdinalIgnoreCase);
        return (saved, languageChanged);
    }

    public Task<UserSettingsDto> UpdateAvatarAsync(Guid userId, UserSettingsDto settings, string mime, string base64)
    {
        settings.UserId = userId;
        settings.AvatarMime = mime;
        settings.AvatarBase64 = base64;
        return _repo.UpdateReturningAsync(settings);
    }

    public Task<UserSettingsDto> RemoveAvatarAsync(Guid userId, UserSettingsDto settings)
    {
        settings.UserId = userId;
        settings.AvatarMime = null;
        settings.AvatarBase64 = null;
        return _repo.UpdateReturningAsync(settings);
    }

    private static string NormalizeCurrency(string? currency)
        => string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();

    private static string NormalizeLanguage(string? lang)
        => string.IsNullOrWhiteSpace(lang) ? "en" : lang.Trim().ToLowerInvariant();
}