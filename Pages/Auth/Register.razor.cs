namespace PersonalCash.Pages.Auth
{
    public partial class Register
    {
        private string email = "";
        private string password = "";
        private string? message;
        private string? error;

        private async Task OnRegister()
        {
            message = null;
            error = null;

            await RunAsync(async () =>
            {
                await Auth.Register(email, password);
                message = "Account created. Check your email and confirm, then login.";


            }, successMessage: "Registration successful. Confirm your email, then login.");
        }
    }
}
