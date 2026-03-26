using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

// Credits https://github.com/patrickgod/BlazorAuthenticationTutorial

namespace Infrastructure.Auth;

public class CustomAuthStateProvider(ILocalStorageService localStorage,BrowserSessionStorage sessionStorage,Supabase.Client client) : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage = localStorage;
    private readonly BrowserSessionStorage _sessionStorage = sessionStorage;
    private const string SessionKey = "supabase_session";
    private readonly Supabase.Client _client = client;

    private Task? _initializeTask;

    private async Task InitializeClientAsync()
    {
        _client.Auth.LoadSession();

        var persistedSession = _client.Auth.CurrentSession;
        var accessToken = persistedSession?.AccessToken;
        var refreshToken = persistedSession?.RefreshToken;

        if (!string.IsNullOrWhiteSpace(accessToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _client.Auth.SetSession(
                    accessToken,
                    refreshToken,
                    forceAccessTokenRefresh: true);
            }
            catch
            {
                await _localStorage.RemoveItemAsync(SessionKey);
                await _sessionStorage.RemoveItemAsync(SessionKey);
            }
        }
        else if (persistedSession is not null)
        {
            await _localStorage.RemoveItemAsync(SessionKey);
            await _sessionStorage.RemoveItemAsync(SessionKey);
        }

        await _client.InitializeAsync();
    }

    private async Task EnsureClientInitializedAsync()
    {
        if (_initializeTask is null)
            _initializeTask = InitializeClientAsync();

        try
        {
            await _initializeTask;
        }
        catch
        {
            _initializeTask = null;
            throw;
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureClientInitializedAsync();

        var identity = new ClaimsIdentity();

        var accessToken = _client.Auth.CurrentSession?.AccessToken;
        if (!string.IsNullOrEmpty(accessToken))
        {
            identity = new ClaimsIdentity(ParseClaimsFromJwt(accessToken), "jwt");
        }

        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public void NotifyAuthStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)
                       ?? new Dictionary<string, object>();

        return keyValuePairs.Select(kvp =>
            new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
