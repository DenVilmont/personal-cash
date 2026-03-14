using Application.Services;
using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared;
using PersonalCash.Shared.Extensions;
using System.Globalization;

namespace PersonalCash.Pages.Reports;

[Authorize]
public partial class ReportsPage : IDisposable
{
    [Inject] private AccountsService AccountsService { get; set; } = default!;
    [Inject] private CategoriesService CategoriesService { get; set; } = default!;
    [Inject] private TransactionService TransactionService { get; set; } = default!;
    [Inject] private AppPageTitleState PageTitleState { get; set; } = default!;

    private List<AccountDto> _accounts = new();
    private List<CategoryDto> _categories = new();

    private readonly Dictionary<Guid, AccountDto> _accountById = new();
    private readonly Dictionary<Guid, CategoryDto> _categoryById = new();

    private List<TransactionDto> _allItems = new();

    private readonly ChartOptions _donutOptions = new()
    {
        ShowLegend = false,
        ChartPalette = new[]
        {
            Colors.Blue.Default,
            Colors.Green.Default,
            Colors.Orange.Default,
            Colors.Red.Default,
            Colors.Purple.Default,
            Colors.Teal.Default,
            Colors.Cyan.Default,
            Colors.Brown.Default,
            Colors.Indigo.Default,
            Colors.Pink.Default,
            Colors.Blue.Darken2,
            Colors.Green.Darken2,
            Colors.Orange.Darken2,
            Colors.Red.Darken2,
            Colors.Purple.Darken2,
            Colors.Teal.Darken2,
            Colors.Cyan.Darken2,
            Colors.Brown.Darken2,
            Colors.Indigo.Darken2,
            Colors.Pink.Darken2
        }
    };

    private readonly LineChartOptions _lineOptions = new()
    {
        ShowDataMarkers = true
    };

    private readonly ChartOptions _barOptions = new()
    {
        ShowLegend = true
    };

    // Primary filters (Tab 1)
    private DateOnly? _primaryFrom;
    private DateOnly? _primaryTo;
    private Guid? _primaryAccountId;
    private DateOnly? _primaryMonth;
    private bool _primaryMonthInitDone;
    private List<DateOnly> _primaryMonthOptions = new();

    // Trend filters (Tab 2/3)
    private List<DateOnly> _trendMonthOptions = new();
    private HashSet<DateOnly> _trendSelectedMonths = new();
    private HashSet<EntryType> _trendSelectedTypes = new() { EntryType.Income, EntryType.Outcome };
    private List<CategoryDto> _trendCategoryOptions = new();
    private HashSet<Guid> _trendSelectedCategoryIds = new();

    // Summary (current month + current balance)
    private Dictionary<string, string> _summaryIncome = new();
    private Dictionary<string, string> _summaryExpenses = new();
    private Dictionary<string, string> _summaryBalance = new();
    private string _summaryCurrency = string.Empty;
    private string _summaryMonthCaption = string.Empty;

    // Category tab
    private List<ReportsCategoryRow> _categoryRows = new();
    private decimal _categoryTotal;
    private string _primaryReportCurrency = string.Empty;
    private List<ChartSeries<double>> _donutSeries = new();
    private string[] _donutLabels = Array.Empty<string>();

    // Trend tabs
    private List<ChartSeries<double>> _trendSeries = new();
    private string[] _trendXAxisLabels = Array.Empty<string>();

    protected override void OnParametersSet()
    {
        PageTitleState.Set(L["Reports_PageTitle"]);
    }

    protected override async Task OnInitializedAsync()
        => await RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        _accounts = await AccountsService.GetSortedAsync();
        _categories = await CategoriesService.GetSortedAsync();
        _allItems = await TransactionService.GetActualAsync();

        _accountById.Clear();
        foreach (var account in _accounts)
            _accountById[account.Id] = account;

        _categoryById.Clear();
        foreach (var category in _categories)
            _categoryById[category.Id] = category;

        RebuildPrimaryMonthOptions();
        InitPrimaryDefaults();
        InitTrendMonths();

        RebuildSummaryForCurrentMonth();
        ApplyPrimaryFilters();
        ApplyTrendFilters();
    }

    private DateTime? PrimaryFromDateTime
    {
        get => _primaryFrom?.ToDateTime(TimeOnly.MinValue);
        set => _primaryFrom = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    private DateTime? PrimaryToDateTime
    {
        get => _primaryTo?.ToDateTime(TimeOnly.MinValue);
        set => _primaryTo = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    private void RebuildPrimaryMonthOptions()
    {
        var months = _allItems
            .Where(x => !x.IsPlanned)
            .Select(x => new DateOnly(x.OccurredOn.Year, x.OccurredOn.Month, 1))
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        if (!months.Contains(currentMonth))
            months.Insert(0, currentMonth);

        _primaryMonthOptions = months;
    }

    private void InitPrimaryDefaults()
    {
        if (_primaryMonthInitDone)
            return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        _primaryMonth = new DateOnly(today.Year, today.Month, 1);
        _primaryMonthInitDone = true;
    }

    private void InitTrendMonths()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        _trendMonthOptions = Enumerable.Range(0, 24)
            .Select(index => currentMonth.AddMonths(-index))
            .ToList();

        _trendSelectedMonths = _trendMonthOptions
            .Take(12)
            .ToHashSet();
    }

    private void RebuildSummaryForCurrentMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var currentMonthItems = _allItems
            .Where(x => !x.IsPlanned)
            .Where(x => x.OccurredOn >= monthStart && x.OccurredOn <= monthEnd)
            .ToList();

        _summaryIncome = currentMonthItems
            .Where(x => x.EntryType == EntryType.Income)
            .GroupBy(x => x.AccountId)
            .Where(g => _accountById.ContainsKey(g.Key))
            .OrderBy(g => _accountById[g.Key].SortOrder)
            .ThenBy(g => _accountById[g.Key].Name)
            .ToDictionary(
                g => _accountById[g.Key].Name,
                g => $"{g.Sum(x => x.Amount)} {_accountById[g.Key].Currency}"
            );

        _summaryExpenses = currentMonthItems
            .Where(x => x.EntryType == EntryType.Outcome)
            .GroupBy(x => x.AccountId)
            .Where(g => _accountById.ContainsKey(g.Key))
            .OrderBy(g => _accountById[g.Key].SortOrder)
            .ThenBy(g => _accountById[g.Key].Name)
            .ToDictionary(
                g => _accountById[g.Key].Name,
                g => $"{g.Sum(x => x.Amount)} {_accountById[g.Key].Currency}"
            );

        _summaryBalance = _accounts
            .Where(x => !x.IsArchived)
            .Where (x => x.ShowBalance)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToDictionary(x => x.Name, y => y.BalanceActual + " " + y.Currency);
    }

    private void ClearPrimaryDates()
    {
        _primaryMonth = null;
        _primaryFrom = null;
        _primaryTo = null;
        ApplyPrimaryFilters();
    }

    private void ResetPrimaryFilters()
    {
        _primaryAccountId = null;

        var today = DateOnly.FromDateTime(DateTime.Today);
        _primaryMonth = new DateOnly(today.Year, today.Month, 1);
        _primaryFrom = null;
        _primaryTo = null;

        ApplyPrimaryFilters();
    }

    private void ApplyPrimaryFilters()
    {
        if (_primaryMonth is not null)
        {
            _primaryFrom = new DateOnly(_primaryMonth.Value.Year, _primaryMonth.Value.Month, 1);
            _primaryTo = _primaryFrom.Value.AddMonths(1).AddDays(-1);
        }

        var filtered = GetPrimaryFilteredTransactions().ToList();

        RebuildCategoryRows(filtered);
        RebuildDonutChart();
    }

    private IEnumerable<TransactionDto> GetPrimaryFilteredTransactions()
    {
        IEnumerable<TransactionDto> query = _allItems.Where(x => !x.IsPlanned);

        if (_primaryFrom is not null)
            query = query.Where(x => x.OccurredOn >= _primaryFrom.Value);

        if (_primaryTo is not null)
            query = query.Where(x => x.OccurredOn <= _primaryTo.Value);

        if (_primaryAccountId is not null)
            query = query.Where(x => x.AccountId == _primaryAccountId.Value);

        return query;
    }

    private void RebuildCategoryRows(IReadOnlyCollection<TransactionDto> filtered)
    {
        var palette = _donutOptions.ChartPalette;

        var grouped = filtered
            .Where(x => x.EntryType == EntryType.Outcome)
            .GroupBy(x => x.CategoryId)
            .Select(group => new
            {
                CategoryId = group.Key,
                Amount = group.Sum(x => x.Amount)
            })
            .Where(x => x.Amount > 0)
            .OrderByDescending(x => x.Amount)
            .ToList();

        _categoryTotal = grouped.Sum(x => x.Amount);
        _primaryReportCurrency = ResolvePrimaryReportCurrency(filtered);

        _categoryRows = grouped
            .Select((row, index) => new ReportsCategoryRow(
                CategoryId: row.CategoryId,
                Label: CategoryName(row.CategoryId),
                Amount: (double)row.Amount,
                Share: _categoryTotal > 0
                    ? Math.Round((double)(row.Amount / _categoryTotal * 100m), 1)
                    : 0d,
                Color: palette[index % palette.Length]))
            .ToList();
    }

    private void RebuildDonutChart()
    {
        if (_categoryRows.Count == 0)
        {
            _donutSeries = new();
            _donutLabels = Array.Empty<string>();
            return;
        }

        _donutLabels = _categoryRows
            .Select(x => x.Label)
            .ToArray();

        _donutSeries = new List<ChartSeries<double>>
        {
            new()
            {
                Data = _categoryRows.Select(x => x.Amount).ToArray()
            }
        };
    }

    private string ResolvePrimaryReportCurrency(IEnumerable<TransactionDto> filtered)
    {
        if (_primaryAccountId is not null &&
            _accountById.TryGetValue(_primaryAccountId.Value, out var selectedAccount))
        {
            return selectedAccount.Currency;
        }

        var currencies = filtered
            .Select(x => x.Currency)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return currencies.Count == 1 ? currencies[0] : string.Empty;
    }

    private Task OnTrendMonthsChanged(IEnumerable<DateOnly> values)
    {
        _trendSelectedMonths = values?.ToHashSet() ?? new HashSet<DateOnly>();
        ApplyTrendFilters();
        return Task.CompletedTask;
    }

    private Task OnTrendTypesChanged(IEnumerable<EntryType> values)
    {
        _trendSelectedTypes = values?.ToHashSet() ?? new HashSet<EntryType>();

        if (_trendSelectedTypes.Count == 0)
            _trendSelectedTypes.Add(EntryType.Outcome);

        ApplyTrendFilters();
        return Task.CompletedTask;
    }

    private Task OnTrendCategoriesChanged(IEnumerable<Guid> values)
    {
        _trendSelectedCategoryIds = values?.ToHashSet() ?? new HashSet<Guid>();
        ApplyTrendFilters();
        return Task.CompletedTask;
    }

    private void ClearTrendCategories()
    {
        _trendSelectedCategoryIds.Clear();
        ApplyTrendFilters();
    }

    private void ResetTrendFilters()
    {
        InitTrendMonths();
        _trendSelectedTypes = new HashSet<EntryType> { EntryType.Income, EntryType.Outcome };
        _trendSelectedCategoryIds.Clear();

        ApplyTrendFilters();
    }

    private void ApplyTrendFilters()
    {
        RebuildTrendCategoryOptions();
        RebuildTrendSeries();
    }

    private void RebuildTrendCategoryOptions()
    {
        var allowedCategoryIds = GetTrendBaseTransactions()
            .Where(x => _trendSelectedTypes.Contains(x.EntryType))
            .Select(x => x.CategoryId)
            .Distinct()
            .ToHashSet();

        _trendCategoryOptions = _categories
            .Where(x => allowedCategoryIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToList();

        _trendSelectedCategoryIds.IntersectWith(allowedCategoryIds);
    }

    private IEnumerable<TransactionDto> GetTrendBaseTransactions()
        => _allItems.Where(x => !x.IsPlanned);

    private void RebuildTrendSeries()
    {
        var months = _trendSelectedMonths
            .OrderBy(x => x)
            .ToList();

        _trendXAxisLabels = months
            .Select(x => x.ToString("MMM yyyy", CultureInfo.CurrentUICulture))
            .ToArray();

        if (months.Count == 0)
        {
            _trendSeries = new();
            return;
        }

        var baseTransactions = GetTrendBaseTransactions()
            .Where(x => _trendSelectedTypes.Contains(x.EntryType));

        if (_trendSelectedCategoryIds.Count > 0)
        {
            var series = new List<ChartSeries<double>>();

            foreach (var categoryId in _trendSelectedCategoryIds)
            {
                var byMonth = baseTransactions
                    .Where(x => x.CategoryId == categoryId)
                    .GroupBy(x => new DateOnly(x.OccurredOn.Year, x.OccurredOn.Month, 1))
                    .ToDictionary(group => group.Key, group => group.Sum(x => x.Amount));

                var data = months
                    .Select(month => byMonth.TryGetValue(month, out var value) ? (double)value : 0d)
                    .ToArray();

                series.Add(new ChartSeries<double>
                {
                    Name = CategoryName(categoryId),
                    Data = data
                });
            }

            _trendSeries = series;
            return;
        }

        var totalSeries = new List<ChartSeries<double>>();

        foreach (var type in _trendSelectedTypes.OrderBy(x => x))
        {
            var byMonth = baseTransactions
                .Where(x => x.EntryType == type)
                .GroupBy(x => new DateOnly(x.OccurredOn.Year, x.OccurredOn.Month, 1))
                .ToDictionary(group => group.Key, group => group.Sum(x => x.Amount));

            var data = months
                .Select(month => byMonth.TryGetValue(month, out var value) ? (double)value : 0d)
                .ToArray();

            totalSeries.Add(new ChartSeries<double>
            {
                Name = type.ToLocalizedString(L),
                Data = data
            });
        }

        _trendSeries = totalSeries;
    }

    private string CategoryName(Guid id)
        => _categoryById.TryGetValue(id, out var category)
            ? category.Name
            : L["Category"];

    public void Dispose()
    {
        PageTitleState.Clear();
    }
}