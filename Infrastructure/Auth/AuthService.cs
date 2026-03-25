using Microsoft.Extensions.Logging;

namespace Infrastructure.Auth;

public class AuthService(
    Supabase.Client client,
    CustomAuthStateProvider customAuthStateProvider,
    AuthPersistenceModeStore persistenceModeStore,
    ILogger<AuthService> logger)
{
    private readonly Supabase.Client client = client;
    private readonly CustomAuthStateProvider customAuthStateProvider = customAuthStateProvider;
    private readonly AuthPersistenceModeStore persistenceModeStore = persistenceModeStore;
    private readonly ILogger<AuthService> logger = logger;

    public async Task Login(string email, string password, bool rememberMe)
    {
        persistenceModeStore.SetRememberMe(rememberMe);

        await client.Auth.SignIn(email, password);

        logger.LogInformation("client.Auth.CurrentUser.Email {Email}", client?.Auth?.CurrentUser?.Email);

        customAuthStateProvider.NotifyAuthStateChanged();
    }

    public async Task Logout()
    {
        await client.Auth.SignOut();
        persistenceModeStore.Clear();
        customAuthStateProvider.NotifyAuthStateChanged();
    }

    public async Task Register(string email, string password)
    {
        await client.Auth.SignUp(
            email,
            password,
            new Supabase.Gotrue.SignUpOptions
            {
                RedirectTo = "https://denvilmont.github.io/personal-cash/"
            });

        logger.LogInformation($"client.Auth.CurrentUser.Email {client?.Auth?.CurrentUser?.Email}");

        customAuthStateProvider.NotifyAuthStateChanged();
    }

    public Task RequestEmailChangeAsync(string email)
    {
        return client.Auth.Update(new Supabase.Gotrue.UserAttributes { Email = email });
    }
}
