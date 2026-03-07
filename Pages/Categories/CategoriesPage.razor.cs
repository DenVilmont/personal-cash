using Application.Services;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Pages.Categories
{
    [Authorize]
    public partial class CategoriesPage
    {
        [Inject] public CategoriesService CategoriesService { get; set; } = default!;

        protected string? _name = null;
        private string _searchString = "";

        protected List<CategoryDto> _items = new();

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
                Snackbar.Add("Not authenticated", Severity.Error);
                return;
            }

            await RunAsync(async () =>
            {
                await CategoriesService.AddAsync(userId, name);
                _name = null;
                await LoadCoreAsync();
            }, successMessage: "Added");
        }

        protected async Task ConfirmDeleteAsync(CategoryDto category)
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            MarkupString msg = (MarkupString)(
                $"{category.Name}<br/><br/>" +
                $"This cannot be undone.");

            // returns Task<bool?> (true = Yes, null/false = Cancel/No) :contentReference[oaicite:0]{index=0}
            bool? confirmed = await DialogService.ShowMessageBoxAsync(
                "Delete category?",
                msg,
                yesText: "Delete",
                cancelText: "Cancel",
                options: options);

            if (confirmed == true)
                await DeleteAsync(category);
        }
        protected Task DeleteAsync(CategoryDto category)
            => RunAsync(async () =>
            {
                await CategoriesService.DeleteAsync(category);
                await LoadCoreAsync();
            }, successMessage: "Deleted");

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

            var dialog = await DialogService.ShowAsync<EditCategoryDialog>("Edit category", parameters, options);
            var result = await dialog.Result;

            if (result is null || result.Canceled)
                return;
            if (result.Data is not CategoryDto updated)
                return;


            await RunAsync(async () =>
            {
                await CategoriesService.UpdateAsync(updated);
                await LoadCoreAsync();
            }, successMessage: "Updated");
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


    }
}
