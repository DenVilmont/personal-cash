using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Auth;

public class AuthService(
    Supabase.Client client,
    CustomAuthStateProvider customAuthStateProvider,
    ILocalStorageService localStorage,
    ILogger<AuthService> logger
    )
{
    private readonly Supabase.Client client = client;
    private readonly CustomAuthStateProvider customAuthStateProvider = customAuthStateProvider;
    private readonly ILocalStorageService localStorage = localStorage;
    private readonly ILogger<AuthService> logger = logger;

    public async Task Login(string email, string password)
    {
        await client.Auth.SignIn(email, password);

        // logger.LogInformation($"instance.Auth.CurrentUser.Id {client?.Auth?.CurrentUser?.Id}");
        logger.LogInformation($"client.Auth.CurrentUser.Email {client?.Auth?.CurrentUser?.Email}");

        customAuthStateProvider.NotifyAuthStateChanged();
    }
    
    public async Task Logout()
    {
        await client.Auth.SignOut();
        customAuthStateProvider.NotifyAuthStateChanged();
    }

    public async Task Register(string email, string password)
    {
        await client.Auth.SignUp(email, password);

        logger.LogInformation($"client.Auth.CurrentUser.Email {client?.Auth?.CurrentUser?.Email}");

        customAuthStateProvider.NotifyAuthStateChanged();
    }

    public Task RequestEmailChangeAsync(string email)
    {
        return client.Auth.Update(new Supabase.Gotrue.UserAttributes { Email = email });
    }
}
