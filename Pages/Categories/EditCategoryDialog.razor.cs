using Microsoft.AspNetCore.Components;
using MudBlazor;
using Domain.Contracts;

namespace PersonalCash.Pages.Categories
{
    public partial class EditCategoryDialog
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter]
        public CategoryDto Tx { get; set; } = default!;

        private string _name = "";

        protected override void OnInitialized()
        {
            _name = Tx.Name;
        }


        private Task SaveAsync()
            => RunAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    Snackbar.Add(L["Categories_NameRequired_ValidationError"], Severity.Warning);
                    return Task.CompletedTask;
                }

                Tx.Name = _name.Trim();
                MudDialog.Close(DialogResult.Ok(Tx));
                return Task.CompletedTask;
            });

        private void Cancel() => MudDialog.Cancel();
    }
}
