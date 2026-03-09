using Application.Services;
using Domain.Contracts;
using Domain.Enums;
using Infrastructure.Auth;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared;

namespace PersonalCash.Pages.Accounts;

[Authorize]
public partial class AccountsPage : IDisposable
{
    [Inject] public AccountsService AccountsService { get; set; } = default!;
    [Inject] public CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;
    [Inject] private AppPageTitleState PageTitleState { get; set; } = default!;

    protected string? _name;
    protected string _currency = "EUR";
    protected bool _showBalance = true;
    protected string _iconKey = AccountIcon.Wallet.ToString();
    protected int _sortOrder = 0;

    protected List<AccountDto> _items = new();

    protected override void OnParametersSet()
    {
        PageTitleState.Set(L["Accounts_PageTitle"]);
    }

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
            Snackbar.Add(L["NotAuthenticated_Error"], Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Snackbar.Add(L["Accounts_NameRequired_ValidationError"], Severity.Warning);
            return;
        }

        if (_sortOrder < 0)
        {
            Snackbar.Add(L["Accounts_SortOrderMustBeValidPositiveNumber_ValidationError"], Severity.Warning);
            return;
        }

        await RunAsync(async () =>
        {
            await AccountsService.AddAsync(
                userId: userId,
                name: name,
                currency: _currency,
                showBalance: _showBalance,
                iconKey: _iconKey,
                sortOrder: _sortOrder);

            _name = null;
            _sortOrder = 0;
            await LoadCoreAsync();
        }, successMessage: L["Added"]);
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
            SortOrder = acc.SortOrder,
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

        var dialog = await DialogService.ShowAsync<EditAccountDialog>(L["Accounts_EditAccount_Title"], parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;
        if (result.Data is not AccountDto updated)
            return;


        await RunAsync(async () =>
        {
            await AccountsService.UpdateAsync(updated);
            await LoadCoreAsync();
        }, successMessage: L["Updated"]);
    }

    protected async Task ConfirmDeleteAsync(AccountDto acc)
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };

        MarkupString msg = (MarkupString)(string.Format(L["Accounts_DeleteMessage"],acc.Name));

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            L["Accounts_DeleteDialog_Title"],
            msg,
            yesText: L["Delete"],
            cancelText: L["Cancel"],
            options: options);

        if (confirmed != true)
            return;

        if (await AccountsService.HasTransactionsAsync(acc.Id))
        {
            await ConfirmArchiveAsync(acc);
            return;
        }

        await DeleteAsync(acc);
    }

    protected Task DeleteAsync(AccountDto acc)
        => RunAsync(async () =>
        {
            await AccountsService.DeleteAsync(acc);
            await LoadCoreAsync();
        }, successMessage: L["Deleted"]);

    protected async Task ConfirmArchiveAsync(AccountDto acc)
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };

        MarkupString msg = (MarkupString)(L["Accounts_DeleteHasTransactions_Message"].Value);

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            L["Accounts_ArchiveDialog_Title"],
            msg,
            yesText: L["Accounts_ArchiveAction"],
            cancelText: L["Cancel"],
            options: options);

        if (confirmed == true)
            await ArchiveAsync(acc);
    }

    protected Task ArchiveAsync(AccountDto acc)
        => RunAsync(async () =>
        {
            await AccountsService.ArchiveAsync(acc);
            await LoadCoreAsync();
        }, successMessage: L["Accounts_Archived"]);

    public void Dispose()
    {
        PageTitleState.Clear();
    }
}
