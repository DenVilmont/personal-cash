using Blazored.LocalStorage;

namespace Infrastructure.Localization
{
    public sealed class CultureService
    {
        private const string StorageKey = "app_culture";
        private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
        {
            "en", "es", "ru"
        };

        private readonly ILocalStorageService _localStorage;

        public CultureService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<string> GetCultureAsync()
        {
            var saved = await _localStorage.GetItemAsStringAsync(StorageKey);
            if (!string.IsNullOrWhiteSpace(saved) && Supported.Contains(saved))
                return saved;

            return "en";
        }

        public async Task SetCultureAsync(string culture)
        {
            if (!Supported.Contains(culture))
                culture = "en";

            await _localStorage.SetItemAsStringAsync(StorageKey, culture);

        }
    }
}
