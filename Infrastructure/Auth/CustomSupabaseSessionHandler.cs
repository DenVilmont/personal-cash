using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Infrastructure.Auth;

public class CustomSupabaseSessionHandler(
    ISyncLocalStorageService localStorage,
    BrowserSessionStorage sessionStorage,
    AuthPersistenceModeStore persistenceModeStore,
    ILogger<CustomSupabaseSessionHandler> logger) : IGotrueSessionPersistence<Session>
{
    private const string SessionKey = "supabase_session";

    private readonly ISyncLocalStorageService _localStorage = localStorage;
    private readonly BrowserSessionStorage _sessionStorage = sessionStorage;
    private readonly AuthPersistenceModeStore _persistenceModeStore = persistenceModeStore;
    private readonly ILogger<CustomSupabaseSessionHandler> _logger = logger;

    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonConvert.SerializeObject(session);

            if (_persistenceModeStore.IsSessionLogin())
            {
                _sessionStorage.SetItem(SessionKey, json);
                _localStorage.RemoveItem(SessionKey);
                _logger.LogInformation("Supabase session saved to sessionStorage.");
                return;
            }

            _localStorage.SetItem(SessionKey, json);
            _sessionStorage.RemoveItem(SessionKey);
            _logger.LogInformation("Supabase session saved to localStorage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Supabase session.");
        }
    }

    public Session? LoadSession()
    {
        try
        {
            string? json = null;

            if (_persistenceModeStore.IsSessionLogin())
            {
                json = _sessionStorage.GetItem(SessionKey);
            }
            else if (_persistenceModeStore.IsPersistentLogin())
            {
                json = _localStorage.GetItem<string>(SessionKey);
            }
            else
            {
                json = _localStorage.GetItem<string>(SessionKey)
                    ?? _sessionStorage.GetItem(SessionKey);
            }

            if (string.IsNullOrWhiteSpace(json))
                return null;

            var session = JsonConvert.DeserializeObject<Session>(json);
            _logger.LogInformation("Supabase session loaded: {HasSession}", session is not null);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Supabase session.");
            return null;
        }
    }

    public void DestroySession()
    {
        _localStorage.RemoveItem(SessionKey);
        _sessionStorage.RemoveItem(SessionKey);
        _persistenceModeStore.Clear();

        _logger.LogInformation("Supabase session removed from storage.");
    }
}