namespace PersonalCash.Pages.Auth;

public partial class Login
{
    protected string email {get; set;} = "admin@denvilmonttestadmin.com";
    protected string password {get; set;} = "UCBD!T#U#qmPi63";

    public async Task OnClickLogin()
    {
        await RunAsync(async () =>
        {
            await AuthService.Login(email, password);
        }, successMessage: "Login successfull");

        NavigationManager.NavigateTo(NavigationManager.BaseUri);
    }
}

