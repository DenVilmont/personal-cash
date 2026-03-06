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
                    return Task.CompletedTask;

                Tx.Name = _name;
                MudDialog.Close(DialogResult.Ok(Tx));
                return Task.CompletedTask;
            });

        private void Cancel() => MudDialog.Cancel();
    }
}
