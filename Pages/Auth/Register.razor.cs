using MudBlazor;

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
                message = L["Account_created_Registration_message"];
            }, successMessage: L["Account_created_Registration_message"]);
        }

        bool isShow;
        InputType PasswordInput = InputType.Password;
        string PasswordInputIcon = Icons.Material.Filled.VisibilityOff;

        void TogglePassword()
        {
            if (isShow)
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
}
