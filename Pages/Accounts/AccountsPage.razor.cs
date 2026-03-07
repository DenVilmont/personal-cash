using Application.Services;
using Domain.Contracts;
using Domain.Enums;
using Infrastructure.Auth;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Pages.Accounts;

[Authorize]
public partial class AccountsPage
{
    [Inject] public AccountsService AccountsService { get; set; } = default!;
    [Inject] public CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;

    protected string? _name;
    protected string _currency = "EUR";
    protected bool _showBalance = true;
    protected string _iconKey = AccountIcon.Wallet.ToString();

    protected List<AccountDto> _items = new();

    protected override async Task OnInitializedAsync()
    {
        if (!CurrentUser.TryGetUserId(out _))
            return;
        _currency = UserSettingsStore.Current?.PreferredCurrency ?? "EUR";
        await LoadAsync();
    }

    protected Task LoadAsync() => RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        _items = await AccountsService.GetSortedAsync();
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
            await AccountsService.AddAsync(
                userId: userId,
                name: name,
                currency: _currency,
                showBalance: _showBalance,
                iconKey: _iconKey);

            _name = null;
            await LoadCoreAsync();
        }, successMessage: "Added");
    }

    protected async Task OpenEditAsync(AccountDto acc)
    {
        var copy = new AccountDto
        {
            Id = acc.Id,
            UserId = acc.UserId,
            Name = acc.Name,
            Currency = acc.Currency,
            ShowBalance = acc.ShowBalance,
            IconKey = acc.IconKey,
            BalanceActual = acc.BalanceActual,
            BalanceExpected = acc.BalanceExpected,
            IsArchived = acc.IsArchived,
            CreatedAt = acc.CreatedAt,
            UpdatedAt = acc.UpdatedAt,
        };

        var parameters = new DialogParameters
        {
            ["Account"] = copy
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<EditAccountDialog>("Edit account", parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;
        if (result.Data is not AccountDto updated)
            return;


        await RunAsync(async () =>
        {
            await AccountsService.UpdateAsync(updated);
            await LoadCoreAsync();
        }, successMessage: "Updated");
    }

    protected async Task ConfirmDeleteAsync(AccountDto acc)
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };

        MarkupString msg = (MarkupString)$"Delete '{acc.Name}'? This cannot be undone.";

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete account?",
            msg,
            yesText: "Delete",
            cancelText: "Cancel",
            options: options);

        if (confirmed == true)
            await DeleteAsync(acc);
    }

    protected Task DeleteAsync(AccountDto acc)
        => RunAsync(async () =>
        {
            await AccountsService.DeleteAsync(acc);
            await LoadCoreAsync();
        }, successMessage: "Deleted");

    protected async Task ConfirmArchiveAsync(AccountDto acc)
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };

        MarkupString msg = (MarkupString)(
            $"It looks like account has transactions. An accuont cannot be deleted while it has transactions.<br/> " +
            $"Do you want to archive account?");

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "Archive account?",
            msg,
            yesText: "Archive",
            cancelText: "Cancel",
            options: options);

        if (confirmed == true)
            await ArchiveAsync(acc);
    }

    protected Task ArchiveAsync(AccountDto acc)
        => RunAsync(async () =>
        {
            await AccountsService.ArchiveAsync(acc);
            await LoadCoreAsync();
        }, successMessage: "Archived");
}
