using Application.Services;
using System.Net;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.FiltersState;
using Domain.Enums;
using Infrastructure.Auth;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared;

namespace PersonalCash.Pages.Transactions;

[Authorize]
public partial class TransactionsPage : IDisposable
{
    [Inject] private TransactionService TxService { get; set; } = default!;
    [Inject] private AccountsService AccountsService { get; set; } = default!;
    [Inject] private CategoriesService CategoriesService { get; set; } = default!;
    [Inject] private CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;
    [Inject] private PageStateService PageStateService { get; set; } = default!;
    [Inject] private AppPageTitleState PageTitleState { get; set; } = default!;

    protected DateOnly? _occurredOn = DateOnly.FromDateTime(DateTime.Today);
    protected decimal? _amount;
    protected EntryType _entryType = EntryType.Outcome; // expense by default
    protected bool _isForPlanning = false;
    protected string _currency = "";
    protected List<AccountDto> _accounts = new();
    protected List<AccountDto> _activeAccounts = new();
    protected List<AccountDto> _regularAccounts = new();
    protected Dictionary<Guid, AccountDto> _accountById = new();
    protected Guid _accountId;
    protected Guid? _destinationAccountId;
    protected List<CategoryDto> _categories = new();
    protected Dictionary<Guid, string> _categoryById = new();
    protected Guid _categoryId;
    private Guid? _lastRegularCategoryId;
    protected string? _note;

    private DateOnly? _fFrom;
    private DateOnly? _fTo;
    private DateOnly? _fMonth;
    private bool _filtersInitDone;
    private List<DateOnly> _monthOptions = new();
    private EntryType? _fEntryType;     // null = all, 0/1/2 = income/outcome/transfer
    private HashSet<Guid> _fCategoryIds = new();     // null = all
    private HashSet<Guid> _fAccountIds = new();
    private bool? _fIsForPlanning;        // null = all, true/false
    private string? _fNote;         // search in note
    private decimal? _fMinAmount;
    private decimal? _fMaxAmount;

    protected List<TransactionDto> _items = new();

    protected override void OnParametersSet()
    {
        PageTitleState.Set(L["Transactions_PageTitle"]);
    }

    protected override async Task OnInitializedAsync()
    {
        if (!CurrentUser.TryGetUserId(out var userId))
            return;

        var userSettings = await UserSettingsStore.GetAsync();
        if (userSettings is not null &&
            !string.IsNullOrWhiteSpace(userSettings.PreferredCurrency))
        {
            _currency = userSettings.PreferredCurrency
                .Trim()
                .ToUpperInvariant();
        }

        await RunAsync(async () =>
        {
            await CategoriesService.EnsureTransferCategoryAsync(userId);
            await LoadCategoriesAsync();
            await LoadCoreAsync();
        });
    }

    protected Guid SelectedAccountId
    {
        get => _accountId;
        set
        {
            _accountId = value;

            if (_accountById.TryGetValue(value, out var acc) &&
                !string.IsNullOrWhiteSpace(acc.Currency))
            {
                _currency = acc.Currency.Trim().ToUpperInvariant();
            }

            if (_entryType == EntryType.Transfer)
                EnsureDestinationStillValid();
        }
    }

    protected EntryType SelectedEntryType
    {
        get => _entryType;
        set
        {
            if (_entryType == value)
                return;

            var wasTransfer = _entryType == EntryType.Transfer;
            var becomesTransfer = value == EntryType.Transfer;

            if (!wasTransfer && becomesTransfer)
            {
                RememberRegularCategory();

                _entryType = EntryType.Transfer;
                _isForPlanning = false;
                _destinationAccountId = null;
                _categoryId = TransferCategory?.Id ?? Guid.Empty;
                return;
            }

            _entryType = value;

            if (wasTransfer && !becomesTransfer)
            {
                _destinationAccountId = null;
                RestoreRegularCategory();
            }
        }
    }

    private void RememberRegularCategory()
    {
        if (_categoryId != Guid.Empty &&
            _categories.Any(x =>
                x.Id == _categoryId &&
                !x.IsTransferCategory))
        {
            _lastRegularCategoryId = _categoryId;
        }
    }

    private string GetAccountName(Guid accountId) =>
    _accountById.TryGetValue(accountId, out var account)
        ? account.Name
        : string.Empty;

    private void RestoreRegularCategory()
    {
        if (_lastRegularCategoryId is Guid previousCategoryId &&
            _categories.Any(x =>
                x.Id == previousCategoryId &&
                !x.IsTransferCategory))
        {
            _categoryId = previousCategoryId;
            return;
        }

        _categoryId = _categories
            .FirstOrDefault(x => !x.IsTransferCategory)?
            .Id ?? Guid.Empty;

        _lastRegularCategoryId =
            _categoryId == Guid.Empty
                ? null
                : _categoryId;
    }

    private void EnsureDestinationStillValid()
    {
        if (_destinationAccountId is null)
            return;

        if (!TransferDestinationAccounts.Any(
                x => x.Id == _destinationAccountId.Value))
        {
            _destinationAccountId = null;
        }
    }

    protected Guid? SelectedDestinationAccountId
    {
        get => _destinationAccountId;
        set => _destinationAccountId = value;
    }

    private CategoryDto? TransferCategory =>
        _categories.FirstOrDefault(x => x.IsTransferCategory);

    protected IReadOnlyList<AccountDto> TransferDestinationAccounts =>
        _regularAccounts
            .Where(x => x.Id != _accountId)
            .Where(x => string.Equals(
                x.Currency,
                _currency,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

    private DateTime? FilterFromDateTime
    {
        get => _fFrom.HasValue ? _fFrom.Value.ToDateTime(TimeOnly.MinValue) : null;
        set
        {
            _fFrom = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
            _fMonth = null;
        }
    }

    private DateTime? FilterToDateTime
    {
        get => _fTo.HasValue ? _fTo.Value.ToDateTime(TimeOnly.MinValue) : null;
        set
        {
            _fTo = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
            _fMonth = null;
        }
    }

    private DateOnly? FilterMonth
    {
        get => _fMonth;
        set
        {
            _fMonth = value;

            if (value is not null)
            {
                _fFrom = null;
                _fTo = null;
            }
        }
    }

    private IReadOnlyCollection<Guid> FilterAccountIds
    {
        get => _fAccountIds;
        set => _fAccountIds = value?.ToHashSet() ?? new HashSet<Guid>();
    }

    private IReadOnlyCollection<Guid> FilterCategoryIds
    {
        get => _fCategoryIds;
        set => _fCategoryIds = value?.ToHashSet() ?? new HashSet<Guid>();
    }

    protected DateTime? OccurredOnPicker
    {
        get => _occurredOn?.ToDateTime(TimeOnly.MinValue);
        set => _occurredOn = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    protected decimal DisplayAmount(TransactionDto tx) =>
    tx.EntryType switch
    {
        EntryType.Income => tx.Amount,
        EntryType.Outcome => -tx.Amount,
        EntryType.Transfer => tx.Amount,
        _ => tx.Amount
    };

    private async Task ReloadAccountsAsync()
    {
        _accounts = await AccountsService.GetSortedAsync();

        _activeAccounts = _accounts
            .Where(x => !x.IsArchived)
            .ToList();

        _regularAccounts = _activeAccounts
            .Where(x => x.AccountType == AccountType.Regular)
            .ToList();

        _accountById = _accounts.ToDictionary(x => x.Id, x => x);
    }

    protected async Task LoadCategoriesAsync()
    {
        _categories = await CategoriesService.GetSortedAsync();
        _categoryById = _categories.ToDictionary(x => x.Id, x => x.Name);

        if (_entryType == EntryType.Transfer)
        {
            _categoryId = TransferCategory?.Id ?? Guid.Empty;
            return;
        }

        _categoryId = _categories
            .FirstOrDefault(x => !x.IsTransferCategory)?
            .Id ?? Guid.Empty;

        _lastRegularCategoryId =
            _categoryId == Guid.Empty
                ? null
                : _categoryId;
    }

    protected Task LoadAsync() => RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        await ReloadAccountsAsync();
        if (_accountId == Guid.Empty || !_regularAccounts.Any(x => x.Id == _accountId))
            SelectedAccountId = _regularAccounts.FirstOrDefault()?.Id ?? Guid.Empty;
        RebuildMonthOptions();
        await InitializeFiltersAsync();
        await ReloadFilteredItemsCoreAsync();
    }
    protected void RefreshAsync()
    {
        _occurredOn = DateOnly.FromDateTime(DateTime.Today);
        _entryType = EntryType.Outcome;
        _destinationAccountId = null;
        SelectedAccountId = _regularAccounts.FirstOrDefault()?.Id ?? Guid.Empty;
        _amount = null;
        _categoryId = _categories.FirstOrDefault(x => !x.IsTransferCategory)?.Id ?? Guid.Empty;
        _lastRegularCategoryId = _categoryId == Guid.Empty ? null : _categoryId;
        _note = null;
        _isForPlanning = false;
    }

    protected async Task AddAsync()
    {
        if (_occurredOn is null)
        {
            Snackbar.Add(L["Transaction_DateRequired_ValidationError"], Severity.Warning);
            return;
        }

        if (_amount is null || _amount <= 0)
        {
            Snackbar.Add(L["Transaction_AmountMustBeValidPositiveNumber_ValidationError"], Severity.Warning);
            return;
        }

        if (_accountId == Guid.Empty)
        {
            Snackbar.Add(L["Transaction_AccountRequired_ValidationError"], Severity.Warning);
            return;
        }

        if (!_regularAccounts.Any(x => x.Id == _accountId))
        {
            Snackbar.Add(L["Transaction_AccountRequired_ValidationError"], Severity.Warning);
            return;
        }

        if (_entryType == EntryType.Transfer)
        {
            if (_destinationAccountId is null ||
                _destinationAccountId.Value == Guid.Empty)
            {
                Snackbar.Add(
                    L["Transaction_DestinationAccountRequired_ValidationError"],
                    Severity.Warning);
                return;
            }

            if (_destinationAccountId.Value == _accountId)
            {
                Snackbar.Add(
                    L["Transaction_TransferAccountsMustDiffer_ValidationError"],
                    Severity.Warning);
                return;
            }

            if (!TransferDestinationAccounts.Any(
                    x => x.Id == _destinationAccountId.Value))
            {
                Snackbar.Add(
                    L["Transaction_DestinationAccountInvalid_ValidationError"],
                    Severity.Warning);
                return;
            }

            var transferCategory = TransferCategory;

            if (transferCategory is null ||
                _categoryId != transferCategory.Id)
            {
                Snackbar.Add(
                    L["Transaction_TransferCategoryRequired_ValidationError"],
                    Severity.Warning);
                return;
            }
        }
        else
        {
            if (_categoryId == Guid.Empty ||
                !_categories.Any(x =>
                    x.Id == _categoryId &&
                    !x.IsTransferCategory))
            {
                Snackbar.Add(
                    L["Transaction_CategoryRequired_ValidationError"],
                    Severity.Warning);
                return;
            }
        }

        if (!CurrentUser.IsAuthenticated)
        {
            Snackbar.Add(L["NotAuthenticated_Error"], Severity.Error);
            return;
        }

        if (!CurrentUser.TryGetUserId(out var userId))
        {
            Snackbar.Add(L["InvalidUserId_Error"], Severity.Error);
            return;
        }

        await RunAsync(async () =>
        {
            var item = new TransactionDto
            {
                OccurredOn = _occurredOn.Value,
                Amount = _amount.Value,
                EntryType = _entryType,
                IsPlanned = _entryType == EntryType.Transfer ? false : _isForPlanning,
                Currency = string.IsNullOrWhiteSpace(_currency) ? "EUR" : _currency.Trim().ToUpperInvariant(),
                AccountId = _accountId,
                DestinationAccountId = _entryType == EntryType.Transfer ? _destinationAccountId : null,
                CategoryId = _categoryId,
                Note = _note,
                UserId = userId
            };

            await TxService.InsertTransactionAndUpdateBalances(item);

            _amount = null;
            _note = null;
            _entryType = EntryType.Outcome;
            _destinationAccountId = null;

            RestoreRegularCategory();

            await ReloadAccountsAsync();
            await ReloadFilteredItemsCoreAsync();
        }, successMessage: L["Added"]);
    }


    protected async Task ConfirmDeleteAsync(TransactionDto tx)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        string details;

        if (tx.EntryType == EntryType.Transfer &&
            tx.DestinationAccountId is Guid destinationAccountId)
        {
            var direction = WebUtility.HtmlEncode(
                $"{GetAccountName(tx.AccountId)} → {GetAccountName(destinationAccountId)}");

            details =
                $"{tx.Amount:N2} {tx.Currency}<br/>" +
                $"{direction}<br/>" +
                $"{tx.OccurredOn:yyyy-MM-dd}";
        }
        else
        {
            details =
                $"{DisplayAmount(tx):N2} {tx.Currency} " +
                $"{tx.OccurredOn:yyyy-MM-dd}";
        }

        MarkupString msg = (MarkupString)(
            $"{details}<br/><br/>" +
            $"{L["Transactions_DeleteDialog_Message"]}");

        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            L["Transactions_DeleteDialog_Title"],
            msg,
            yesText: L["Delete"],
            cancelText: L["Cancel"],
            options: options);

        if (confirmed == true)
            await DeleteAsync(tx);
    }

    protected Task DeleteAsync(TransactionDto tx)
        => RunAsync(async () =>
        {
            await TxService.DeleteTransactionAndUpdateBalances(tx);
            await ReloadAccountsAsync();
            await ReloadFilteredItemsCoreAsync();
        }, successMessage: L["Deleted"]);

    protected async Task OpenEditAsync(TransactionDto tx)
    {
        var copy = new TransactionDto
        {
            Id = tx.Id,
            UserId = tx.UserId,
            OccurredOn = tx.OccurredOn,
            Amount = tx.Amount,
            EntryType = tx.EntryType,
            IsPlanned = tx.IsPlanned,
            AccountId = tx.AccountId,
            DestinationAccountId = tx.DestinationAccountId,
            CategoryId = tx.CategoryId,
            Currency = tx.Currency,
            Note = tx.Note,
            CreatedAt = tx.CreatedAt
        };

        var parameters = new DialogParameters
        {
            ["Tx"] = copy,
            ["Categories"] = _categories,
            ["Accounts"] = _accounts
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<EditTransactionDialog>(L["Transactions_EditTransaction_Title"], parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;
        if (result.Data is not TransactionDto updated)
            return;

        await RunAsync(async () =>
        {
            await TxService.UpdateTransactionAndUpdateBalances(tx, updated);
            await ReloadAccountsAsync();
            await ReloadFilteredItemsCoreAsync();
        }, successMessage: L["Updated"]);
    }

    protected async Task OpenRealizePlannedAsync(TransactionDto tx)
    {
        var copy = new TransactionDto
        {
            Id = tx.Id,
            UserId = tx.UserId,
            OccurredOn = tx.OccurredOn,
            Amount = tx.Amount,
            EntryType = tx.EntryType,
            IsPlanned = tx.IsPlanned,
            AccountId = tx.AccountId,
            DestinationAccountId = tx.DestinationAccountId,
            CategoryId = tx.CategoryId,
            Currency = tx.Currency,
            Note = tx.Note,
            CreatedAt = tx.CreatedAt
        };

        var parameters = new DialogParameters
        {
            ["Tx"] = copy,
            ["Categories"] = _categories,
            ["Accounts"] = _accounts
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<RealizePlannedTransactionDialog>(L["Transactions_RealizePlannedTransaction_Title"], parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;
        if (result.Data is not TransactionDto updated)
            return;

        await RunAsync(async () =>
        {
            if (updated.Amount >= 0)
            {
                await TxService.UpdateTransactionAndUpdateBalances(tx, updated);
            }
            else
            {
                await TxService.DeleteTransactionAndUpdateBalances(updated);
            }
            await ReloadAccountsAsync();
            await ReloadFilteredItemsCoreAsync();
        }, successMessage: L["Transactions_TransactionCompleted"]);
    }

    private void RebuildMonthOptions()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        _monthOptions.Clear();
        for (var m = currentMonth; m >= currentMonth.AddMonths(-23); m = m.AddMonths(-1))
            _monthOptions.Add(m);
    }

    private TransactionsFilterStateDto BuildCurrentFilterState()
    {
        return new TransactionsFilterStateDto
        {
            From = _fFrom,
            To = _fTo,
            Month = _fMonth,
            AccountIds = _fAccountIds.ToList(),
            CategoryIds = _fCategoryIds.ToList(),
            SelectedEntryType = _fEntryType,
            IsForPlanning = _fIsForPlanning,
            Note = string.IsNullOrWhiteSpace(_fNote) ? null : _fNote.Trim(),
            MinAmount = _fMinAmount,
            MaxAmount = _fMaxAmount
        };
    }

    private void ApplyFilterState(TransactionsFilterStateDto state)
    {
        _fFrom = state.From;
        _fTo = state.To;

        _fMonth = state.Month is not null && _monthOptions.Contains(state.Month.Value)
            ? state.Month
            : null;

        var regularAccountIds = _regularAccounts
            .Select(x => x.Id)
            .ToHashSet();

        _fAccountIds = (state.AccountIds ?? new List<Guid>())
            .Where(id => regularAccountIds.Contains(id))
            .ToHashSet();

        _fCategoryIds = (state.CategoryIds ?? new List<Guid>())
            .Where(id => _categoryById.ContainsKey(id))
            .ToHashSet();

        _fEntryType = state.SelectedEntryType;
        _fIsForPlanning = state.IsForPlanning;
        _fNote = string.IsNullOrWhiteSpace(state.Note) ? null : state.Note.Trim();
        _fMinAmount = state.MinAmount;
        _fMaxAmount = state.MaxAmount;

        if (_fMonth is not null)
        {
            _fFrom = null;
            _fTo = null;
        }
    }

    private async Task InitializeFiltersAsync()
    {
        if (_filtersInitDone)
            return;

        var saved = await PageStateService.LoadAsync<TransactionsFilterStateDto>(PageStateKeys.Transactions);

        if (saved is not null)
            ApplyFilterState(saved);
        else
            ApplyDefaultFilters();

        _filtersInitDone = true;
    }

    private async Task ReloadFilteredItemsCoreAsync()
    {
        var filter = BuildCurrentFilterState();
        _items = await TxService.GetFilteredAsync(filter);
    }
    private Task ApplyFiltersAsync()
        => RunAsync(ReloadFilteredItemsCoreAsync);

    private void ApplyDefaultFilters()
    {
        _fFrom = null;
        _fTo = null;
        _fEntryType = null;
        _fCategoryIds = new HashSet<Guid>();
        _fAccountIds = new HashSet<Guid>();
        _fIsForPlanning = null;
        _fNote = null;
        _fMinAmount = null;
        _fMaxAmount = null;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        if(_monthOptions.Count == 0)
        {
            _fMonth = null;
        }
        else
        {
            _fMonth = _monthOptions.Contains(currentMonth)
            ? currentMonth
            : _monthOptions.FirstOrDefault();
        }
    }

    private Task ClearFilters()
        => RunAsync(async () =>
        {
            ApplyDefaultFilters();
            await ReloadFilteredItemsCoreAsync();
        }, successMessage: L["Filter_Cleared_InfoMessage"], Severity.Info);

    private async Task SaveFiltersAsync()
    {
        if (!CurrentUser.TryGetUserId(out var userId))
        {
            Snackbar.Add(L["InvalidUserId_Error"], Severity.Error);
            return;
        }

        await RunAsync(async () =>
        {
            await PageStateService.SaveAsync(userId, PageStateKeys.Transactions, BuildCurrentFilterState());
        }, successMessage: L["Filter_Saved_InfoMessage"], Severity.Info);
    }

    private async Task ResetFiltersAsync()
    {
        await RunAsync(async () =>
        {
            await PageStateService.DeleteAsync(PageStateKeys.Transactions);
            ApplyDefaultFilters();
            await ReloadFilteredItemsCoreAsync();
        }, successMessage: L["Filter_ResetCompleted_InfoMessage"], Severity.Info);
    }

    public void Dispose()
    {
        PageTitleState.Clear();
    }
}
