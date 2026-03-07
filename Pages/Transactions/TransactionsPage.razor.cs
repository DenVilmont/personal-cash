using Application.Services;
using Domain.Contracts;
using Domain.Enums;
using Infrastructure.Auth;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Pages.Transactions;

[Authorize]
public partial class TransactionsPage
{
    [Inject] private TransactionService TxService { get; set; } = default!;
    [Inject] private AccountsService AccountsService { get; set; } = default!;
    [Inject] private CategoriesService CategoriesService { get; set; } = default!;
    [Inject] private CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;

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
    protected string _categoryName = "";
    protected string? _note;

    private DateOnly? _fFrom;
    private DateOnly? _fTo;
    private EntryType? _fEntryType;     // null = all, 0/1 = income/outcome
    private Guid? _fCategoryId;     // null = all
    private bool? _fIsForPlanning;        // null = all, true/false
    private string? _fNote;         // search in note
    private decimal? _fMinAmount;
    private decimal? _fMaxAmount;
    private Guid? _fAccountId;
    private DateOnly? _fMonth;
    private bool _monthInitDone;
    private List<DateOnly> _monthOptions = new();

    private List<TransactionDto> _allItems = new();
    protected List<TransactionDto> _items = new();

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

    private DateTime? FilterFromDateTime
    {
        get => _fFrom.HasValue ? _fFrom.Value.ToDateTime(TimeOnly.MinValue) : null;
        set => _fFrom = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    private DateTime? FilterToDateTime
    {
        get => _fTo.HasValue ? _fTo.Value.ToDateTime(TimeOnly.MinValue) : null;
        set => _fTo = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
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
            _categoryName = CategoryName(_categoryId);
        }
        else
        {
            _categoryId = Guid.Empty;
            _categoryName = "";
        }
        

        _accounts = await AccountsService.GetSortedAsync();
        _activeAccounts = _accounts.Where(q => !q.IsArchived).ToList();

        _accountById = _accounts.ToDictionary(x => x.Id, x => x);

        // Prefer the first non-archived account, otherwise take the first one.
        _accountId = _accounts.FirstOrDefault(a => !a.IsArchived)?.Id
                     ?? _accounts.FirstOrDefault()?.Id
                     ?? Guid.Empty;

        // Keep currency in sync with the selected account.
        if (_accountById.TryGetValue(_accountId, out var acc))
            _currency = acc.Currency;
    }

    protected string GetAccountName(Guid id) => _accountById.TryGetValue(id, out var a) ? a.Name : "";
    protected AccountIcon GetAccountIcon(Guid id) => AccountIconExtensions.FromDbKey(_accountById.TryGetValue(id, out var a) ? a.IconKey : "");
    protected string CategoryName(Guid id) => _categoryById.TryGetValue(id, out var name) ? name : "";

    protected Task LoadAsync() => RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        _amount = 0;
        _entryType = EntryType.Outcome;
        _isForPlanning = false;
        _note = "";

        _allItems = await TxService.GetAllAsync();
        RebuildMonthOptions();

        _activeAccounts = await AccountsService.GetActiveAsync();


        if (!_monthInitDone)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);
            _fMonth = _monthOptions.Contains(currentMonth)
                ? currentMonth
                : _monthOptions.FirstOrDefault();

            // если список пуст — не ставим месяц
            if (_monthOptions.Count == 0)
                _fMonth = null;
            _monthInitDone = true;
        }
        ApplyFilters();
    }

    private string TxRowClass(TransactionDto tx, int rowNumber)
    {
        if (tx.IsPlanned)
            return "tx-planned";

        return tx.EntryType switch
        {
            EntryType.Income => "tx-income",
            EntryType.Outcome => "tx-outcome",
            _ => ""
        };
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
            Snackbar.Add(L["Transaction_AmountMustBePositive_ValidationError"], Severity.Warning);
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
            $"This cannot be undone.");

        // returns Task<bool?> (true = Yes, null/false = Cancel/No) :contentReference[oaicite:0]{index=0}
        bool? confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete transaction?",
            msg,
            yesText: "Delete",
            cancelText: "Cancel",
            options: options);

        if (confirmed == true)
            await DeleteAsync(tx);
    }

    protected Task DeleteAsync(TransactionDto tx)
        => RunAsync(async () =>
        {
            await TxService.DeleteTransactionAndUpdateBalances(tx);
            await LoadCoreAsync();
        }, successMessage: "Deleted");

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

        var dialog = await DialogService.ShowAsync<EditTransactionDialog>("Edit transaction", parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;
        if (result.Data is not TransactionDto updated)
            return;

        await RunAsync(async () =>
        {
            await TxService.UpdateTransactionAndUpdateBalances(tx, updated);
            await LoadCoreAsync();
        }, successMessage: "Updated");
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

        var dialog = await DialogService.ShowAsync<RealizePlannedTransactionDialog>("Realize planned transaction", parameters, options);
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
        }, successMessage: "Realized");
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

    private void ApplyFilters()
    {
        IEnumerable<TransactionDto> q = _allItems;
        if (_fMonth is not null)
        {
            _fFrom = new DateOnly(_fMonth.Value.Year, _fMonth.Value.Month, 1);
            _fTo = _fFrom.Value.AddMonths(1).AddDays(-1);
        }
        if (_fFrom is not null)
            q = q.Where(x => x.OccurredOn >= _fFrom.Value);

        if (_fTo is not null)
            q = q.Where(x => x.OccurredOn <= _fTo.Value);

        if (_fEntryType is not null)
            q = q.Where(x => x.EntryType == _fEntryType.Value);

        if (_fCategoryId is not null)
            q = q.Where(x => x.CategoryId == _fCategoryId.Value);

        if (_fAccountId is not null)
            q = q.Where(x => x.AccountId == _fAccountId.Value);

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
    private void CrearFDates()
    {
        _fMonth = null;
        _fFrom = null;
        _fTo = null;
        ApplyFilters();
    }

    private void ResetFilters()
    {
        _fFrom = null;
        _fTo = null;
        _fMonth = null;
        _fEntryType = null;
        _fCategoryId = null;
        _fIsForPlanning = null;
        _fNote = null;
        _fMinAmount = null;
        _fMaxAmount = null;
        

        ApplyFilters();
    }
}
