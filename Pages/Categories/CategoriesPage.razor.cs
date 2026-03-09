using Application.Services;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared;

namespace PersonalCash.Pages.Categories
{
    [Authorize]
    public partial class CategoriesPage : IDisposable
    {
        [Inject] public CategoriesService CategoriesService { get; set; } = default!;
        [Inject] private AppPageTitleState PageTitleState { get; set; } = default!;

        protected string? _name = null;
        private string _searchString = "";

        protected List<CategoryDto> _items = new();

        protected override void OnParametersSet()
        {
            PageTitleState.Set(L["Categories_PageTitle"]);
        }

        protected override async Task OnInitializedAsync()
        {
            if (!CurrentUser.TryGetUserId(out _))
                return;

            await LoadAsync();
        }

        protected Task LoadAsync() => RunAsync(LoadCoreAsync);

        private async Task LoadCoreAsync()
        {
            _name = "";

            _items = await CategoriesService.GetSortedAsync();
        }

        protected async Task AddAsync()
        {
            var name = (_name ?? "").Trim();

            if (!CurrentUser.TryGetUserId(out var userId))
            {
                Snackbar.Add(L["NotAuthenticated_Error"], Severity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                Snackbar.Add(L["Categories_NameRequired_ValidationError"], Severity.Warning);
                return;
            }

            await RunAsync(async () =>
            {
                await CategoriesService.AddAsync(userId, name);
                _name = null;
                await LoadCoreAsync();
            }, successMessage: L["Added"]);
        }

        protected async Task ConfirmDeleteAsync(CategoryDto category)
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            MarkupString msg = (MarkupString)(string.Format(L["Categories_DeleteMessage"], category.Name));

            bool? confirmed = await DialogService.ShowMessageBoxAsync(
                L["Categories_DeleteDialog_Title"],
                msg,
                yesText: L["Delete"],
                cancelText: L["Cancel"],
                options: options);

            if (confirmed == true)
                await DeleteAsync(category);
        }
        protected Task DeleteAsync(CategoryDto category)
            => RunAsync(async () =>
            {
                await CategoriesService.DeleteAsync(category);
                await LoadCoreAsync();
            }, successMessage: L["Deleted"]);

        protected async Task OpenEditAsync(CategoryDto category)
        {
            var copy = new CategoryDto
            {
                Id = category.Id,
                UserId = category.UserId,
                Name = category.Name,
                CreatedAt = category.CreatedAt
            };

            var parameters = new DialogParameters
            {
                ["Tx"] = copy,
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = true
            };

            var dialog = await DialogService.ShowAsync<EditCategoryDialog>(L["Categories_EditCategory_Title"], parameters, options);
            var result = await dialog.Result;

            if (result is null || result.Canceled)
                return;
            if (result.Data is not CategoryDto updated)
                return;


            await RunAsync(async () =>
            {
                await CategoriesService.UpdateAsync(updated);
                await LoadCoreAsync();
            }, successMessage: L["Updated"]);
        }

        private bool SearchInTable(CategoryDto category)
        {
            if (string.IsNullOrWhiteSpace(_searchString))
                return true;
            if (category.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
        private async Task ClearSearch()
        {
            _searchString = string.Empty;
            await LoadAsync();
        }

        public void Dispose()
        {
            PageTitleState.Clear();
        }
    }
}
