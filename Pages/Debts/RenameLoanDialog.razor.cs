using Microsoft.AspNetCore.Components;
using MudBlazor;
using Application.Common;

namespace PersonalCash.Pages.Debts
{
    public partial class RenameLoanDialog
    {
        [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public string Name { get; set; } = "";

        private string _name = "";

        protected override void OnParametersSet()
        {
            _name = Name ?? "";
        }

        private void Cancel() => MudDialog.Cancel();

        private Task SaveAsync()
            => RunAsync(() =>
            {
                var v = (_name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(v))
                    throw new AppValidationException("Enter loan name");

                MudDialog.Close(DialogResult.Ok(v));
                return Task.CompletedTask;
            });
    }
}
