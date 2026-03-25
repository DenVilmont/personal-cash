using MudBlazor;
using PersonalCash.Configuration;

namespace PersonalCash.Pages.Auth;

public partial class Login
{
    protected string email {get; set;} = string.Empty;
    protected string password {get; set;} = string.Empty;
    protected bool rememberMe { get; set; } = false;

    public async Task OnSubmitLogin()
    {
        await RunAsync(async () =>
        {
            await AuthService.Login(email, password, rememberMe);
            NavigationManager.NavigateTo("transactions");
        }, successMessage: L["Login_successful"]);
    }

    public async Task OnClickDemoLogin()
    {
        await RunAsync(async () =>
        {
            await AuthService.Login(PublicDemoAccount.Email, PublicDemoAccount.Password, false);
            NavigationManager.NavigateTo("transactions");
        }, successMessage: L["Demo_login_successful"]);
    }

    bool isShow;
    InputType PasswordInput = InputType.Password;
    string PasswordInputIcon = Icons.Material.Filled.VisibilityOff;

    void TogglePassword()
    {
        if(isShow)
        {
            isShow = false;
            PasswordInputIcon = Icons.Material.Filled.VisibilityOff;
            PasswordInput = InputType.Password;
        }
        else
        {
            isShow = true;
            PasswordInputIcon = Icons.Material.Filled.Visibility;
            PasswordInput = InputType.Text;
        }
    }
}

