using Application.Services;
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
    protected decimal _amount;
    protected EntryType _entryType = EntryType.Outcome; // expense by default
    protected bool _isForPlanning = false;
    protected string _currency = "";
    protected List<AccountDto> _accounts = new();
    protected List<AccountDto> _activeAccounts = new();
    protected Dictionary<Guid, AccountDto> _accountById = new();
    protected Guid _accountId;
    protected List<CategoryDto> _categories = new();
    protected Dictionary<Guid, string> _categoryById = new();
    protected Guid _categoryId;
    protected string? _note;

    private DateOnly? _fFrom;
    private DateOnly? _fTo;
    private DateOnly? _fMonth;
    private bool _filtersInitDone;
    private List<DateOnly> _monthOptions = new();
    private EntryType? _fEntryType;     // null = all, 0/1 = income/outcome
    private HashSet<Guid> _fCategoryIds = new();     // null = all
    private HashSet<Guid> _fAccountIds = new();
    private bool? _fIsForPlanning;        // null = all, true/false
    private string? _fNote;         // search in note
    private decimal? _fMinAmount;
    private decimal? _fMaxAmount;

    private List<TransactionDto> _allItems = new();
    protected List<TransactionDto> _items = new();

    protected override void OnParametersSet()
    {
        PageTitleState.Set(L["Transactions_PageTitle"]);
    }

    protected override async Task OnInitializedAsync()
    {
        if (!CurrentUser.TryGetUserId(out _))
            return;

        var userSettings = await UserSettingsStore.GetAsync();
        if (userSettings is not null && !string.IsNullOrWhiteSpace(userSettings.PreferredCurrency))
            _currency = userSettings.PreferredCurrency.Trim().ToUpperInvariant();
        
        await LoadCategoriesAsync();
        await LoadAsync();
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
        }
    }

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

    protected decimal SignedAmount(TransactionDto t) => t.EntryType == EntryType.Outcome ? -t.Amount : t.Amount;

    protected async Task LoadCategoriesAsync()
    {
        _categories = await CategoriesService.GetSortedAsync();
        _categoryById = _categories.ToDictionary(x => x.Id, x => x.Name);
        if (_categories.Count > 0)
        {
            _categoryId = _categories.First().Id;
        }
        else
        {
            _categoryId = Guid.Empty;
        }
        

        _accounts = await AccountsService.GetSortedAsync();
        _activeAccounts = _accounts.Where(q => !q.IsArchived).ToList();

        _accountById = _accounts.ToDictionary(x => x.Id, x => x);

        SelectedAccountId = _activeAccounts.FirstOrDefault()?.Id ?? Guid.Empty;
    }

    protected Task LoadAsync() => RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        _allItems = await TxService.GetAllAsync();
        RebuildMonthOptions();

        _activeAccounts = await AccountsService.GetActiveAsync();

        await InitializeFiltersAsync();
        ApplyFilters();
    }
    protected void RefreshAsync()
    {
        _occurredOn = DateOnly.FromDateTime(DateTime.Today);
        _entryType = EntryType.Outcome;
        SelectedAccountId = _activeAccounts.FirstOrDefault()?.Id ?? Guid.Empty;
        _amount = 0m;
        _categoryId = _categories.First().Id;
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

        if (_amount <= 0)
        {
            Snackbar.Add(L["Transaction_AmountMustBeValidPositiveNumber_ValidationError"], Severity.Warning);
            return;
        }

        if (_accountId == Guid.Empty)
        {
            Snackbar.Add(L["Transaction_AccountRequired_ValidationError"], Severity.Warning);
            return;
        }

        if (_categoryId == Guid.Empty)
        {
            Snackbar.Add(L["Transaction_CategoryRequired_ValidationError"], Severity.Warning);
            return;
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
                Amount = _amount,
                EntryType = _entryType,
                IsPlanned = _isForPlanning,
                Currency = string.IsNullOrWhiteSpace(_currency) ? "EUR" : _currency.Trim().ToUpperInvariant(),
                AccountId = _accountId,
                CategoryId = _categoryId,
                Note = _note,
                UserId = userId
            };

            await TxService.InsertTransactionAndUpdateBalances(item);

            _amount = 0m;
            _note = null;
            await LoadCoreAsync();
        }, successMessage: L["Added"]);
    }


    protected async Task ConfirmDeleteAsync(TransactionDto tx)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        MarkupString msg = (MarkupString)(
            $"{SignedAmount(tx)} {tx.Currency}  {tx.OccurredOn:yyyy-MM-dd}<br/><br/>" +
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
            await LoadCoreAsync();
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
            await LoadCoreAsync();
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
            await LoadCoreAsync();
        }, successMessage: L["Transactions_TransactionCompleted"]);
    }

    private void RebuildMonthOptions()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var startMonth = currentMonth.AddMonths(-11);

        if (_allItems.Count > 0)
        {
            var minDate = _allItems.Min(x => x.OccurredOn);
            var minMonth = new DateOnly(minDate.Year, minDate.Month, 1);
            if (minMonth > startMonth)
                startMonth = minMonth;
        }

        _monthOptions.Clear();

        for (var m = currentMonth; m >= startMonth; m = m.AddMonths(-1))
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

        _fAccountIds = (state.AccountIds ?? new List<Guid>())
            .Where(id => _accountById.ContainsKey(id))
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

    private void ApplyFilters()
    {
        IEnumerable<TransactionDto> q = _allItems;

        DateOnly? effectiveFrom = _fFrom;
        DateOnly? effectiveTo = _fTo;

        if (_fMonth is not null)
        {
            effectiveFrom = new DateOnly(_fMonth.Value.Year, _fMonth.Value.Month, 1);
            effectiveTo = effectiveFrom.Value.AddMonths(1).AddDays(-1);
        }

        if (effectiveFrom is not null)
            q = q.Where(x => x.OccurredOn >= effectiveFrom.Value);

        if (effectiveTo is not null)
            q = q.Where(x => x.OccurredOn <= effectiveTo.Value);

        if (_fEntryType is not null)
            q = q.Where(x => x.EntryType == _fEntryType.Value);

        if (_fCategoryIds.Count > 0)
            q = q.Where(x => _fCategoryIds.Contains(x.CategoryId));

        if (_fAccountIds.Count > 0)
            q = q.Where(x => _fAccountIds.Contains(x.AccountId));

        if (_fIsForPlanning is not null)
            q = q.Where(x => x.IsPlanned == _fIsForPlanning.Value);

        if (!string.IsNullOrWhiteSpace(_fNote))
        {
            var s = _fNote.Trim();
            q = q.Where(x => !string.IsNullOrWhiteSpace(x.Note) &&
                             x.Note.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (_fMinAmount is not null)
            q = q.Where(x => x.Amount >= _fMinAmount.Value);

        if (_fMaxAmount is not null)
            q = q.Where(x => x.Amount <= _fMaxAmount.Value);

        q = q.OrderByDescending(x => x.OccurredOn).ThenByDescending(x => x.CreatedAt);

        _items = q.ToList();
    }

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

    private void ClearFilters()
    {
        _fFrom = null;
        _fTo = null;
        _fMonth = null;
        _fEntryType = null;
        _fAccountIds = new HashSet<Guid>();
        _fCategoryIds = new HashSet<Guid>();
        _fIsForPlanning = null;
        _fNote = null;
        _fMinAmount = null;
        _fMaxAmount = null;

        ApplyFilters();
        Snackbar.Add(L["Filter_Cleared_InfoMessage"], Severity.Info);
    }

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
            ApplyFilters();
        }, successMessage: L["Filter_ResetCompleted_InfoMessage"], Severity.Info);
    }

    public void Dispose()
    {
        PageTitleState.Clear();
    }
}
