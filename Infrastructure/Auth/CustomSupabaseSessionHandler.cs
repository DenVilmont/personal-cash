using Blazored.LocalStorage;
using Newtonsoft.Json;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Auth;

public class CustomSupabaseSessionHandler(ISyncLocalStorageService localStorage, ILogger<CustomSupabaseSessionHandler> logger) : IGotrueSessionPersistence<Session>
{
    private const string SessionKey = "supabase_session";
    private readonly ISyncLocalStorageService _localStorage = localStorage;
    private readonly ILogger<CustomSupabaseSessionHandler> _logger = logger;

    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonConvert.SerializeObject(session);
            _localStorage.SetItem(SessionKey, json);
            _logger.LogInformation("Supabase session saved to localStorage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Supabase session to localStorage.");
        }
    }

    public Session? LoadSession()
    {
        try
        {
            var json = _localStorage.GetItem<string>(SessionKey);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var session = JsonConvert.DeserializeObject<Session>(json);
            _logger.LogInformation("Supabase session loaded from localStorage: {HasSession}", session != null);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Supabase session from localStorage.");
            return null;
        }
    }

    public void DestroySession()
    {
        _localStorage.RemoveItem(SessionKey);
        _logger.LogInformation("Supabase session removed from localStorage.");
    }
}
