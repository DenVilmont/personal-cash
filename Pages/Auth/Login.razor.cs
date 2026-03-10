using PersonalCash.Configuration;

namespace PersonalCash.Pages.Auth;

public partial class Login
{
    protected string email {get; set;} = string.Empty;
    protected string password {get; set;} = string.Empty;

    public async Task OnSubmitLogin()
    {
        await RunAsync(async () =>
        {
            await AuthService.Login(email, password);
            NavigationManager.NavigateTo("transactions");
        }, successMessage: L["Login_successful"]);
    }

    public async Task OnClickDemoLogin()
    {
        await RunAsync(async () =>
        {
            await AuthService.Login(PublicDemoAccount.Email, PublicDemoAccount.Password);
            NavigationManager.NavigateTo("transactions");
        }, successMessage: L["Demo_login_successful"]);
    }
}

