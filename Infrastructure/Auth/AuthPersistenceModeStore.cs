using Blazored.LocalStorage;

namespace Infrastructure.Auth;

public sealed class AuthPersistenceModeStore(
    ISyncLocalStorageService localStorage,
    BrowserSessionStorage sessionStorage)
{
    private const string PersistenceModeKey = "auth_persistence_mode";

    private readonly ISyncLocalStorageService _localStorage = localStorage;
    private readonly BrowserSessionStorage _sessionStorage = sessionStorage;

    public void SetRememberMe(bool rememberMe)
    {
        Clear();

        if (rememberMe)
        {
            _localStorage.SetItem(PersistenceModeKey, "local");
            return;
        }

        _sessionStorage.SetItem(PersistenceModeKey, "session");
    }

    public bool IsPersistentLogin()
        => string.Equals(
            _localStorage.GetItem<string>(PersistenceModeKey),
            "local",
            StringComparison.Ordinal);

    public bool IsSessionLogin()
        => string.Equals(
            _sessionStorage.GetItem(PersistenceModeKey),
            "session",
            StringComparison.Ordinal);

    public void Clear()
    {
        _localStorage.RemoveItem(PersistenceModeKey);
        _sessionStorage.RemoveItem(PersistenceModeKey);
    }
}