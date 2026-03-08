using Application.Services;
using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared.Extensions;

namespace PersonalCash.Pages.Reports;

[Authorize]
public partial class ReportsPage
{
    [Inject] private AccountsService AccountsService { get; set; } = default!;
    [Inject] private CategoriesService CategoriesService { get; set; } = default!;
    [Inject] private TransactionService TransactionService { get; set; } = default!;

    private List<AccountDto> _accounts = new();
    private List<CategoryDto> _categories = new();
    private readonly Dictionary<Guid, AccountDto> _accountById = new();
    private readonly Dictionary<Guid, CategoryDto> _categoryById = new();

    private List<TransactionDto> _allItems = new();

    private readonly LineChartOptions _lineOptions = new()
    {
        ShowDataMarkers = true
    };

    // ---------------------------
    // DONUT filters (same as Transactions, but without EntryType; Pie always uses Outcome)
    // ---------------------------
    private DateOnly? _pFrom;
    private DateOnly? _pTo;
    private Guid? _pAccountId;
    private DateOnly? _pMonth;
    private bool _pMonthInitDone;
    private List<DateOnly> _pMonthOptions = new();
    private sealed record DonutLegendRow(string Label, double Value, string Color);

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
        Colors.Pink.Darken2,
    }
    };

    private List<DonutLegendRow> _donutLegend = new();
    private double DonutTotal => _donutLegend.Sum(x => x.Value);

    protected List<ChartSeries<double>> _donutSeries = new();
    protected string[] _donutLabels = Array.Empty<string>();

    // ---------------------------
    // LINE filters
    // ---------------------------
    private List<DateOnly> _lineMonthOptions = new();
    private HashSet<DateOnly> _lineSelectedMonths = new();
    private HashSet<EntryType> _lineSelectedTypes = new() { EntryType.Outcome };
    private List<CategoryDto> _lineCategoryOptions = new();
    private HashSet<Guid> _lineSelectedCategoryIds = new();

    protected List<ChartSeries<double>> _lineSeries = new();
    protected string[] _lineXAxisLabels = Array.Empty<string>();

    protected override async Task OnInitializedAsync() => await RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        _accounts = await AccountsService.GetSortedAsync();

        _categories = await CategoriesService.GetSortedAsync();

        _allItems = await TransactionService.GetActualAsync();

        _accountById.Clear();
        foreach (var a in _accounts)
            _accountById[a.Id] = a;

        _categoryById.Clear();
        foreach (var c in _categories)
            _categoryById[c.Id] = c;

        RebuildDonutMonthOptions();
        InitDonutDefaults();
        ApplyDonutFilters();

        InitLineMonths();
        RebuildLineCategoryOptions();
        RebuildLineChart();
    }


    // ---------------------------
    // DONUT helpers
    // ---------------------------
    private DateTime? DonutFilterFromDateTime
    {
        get => _pFrom.HasValue ? _pFrom.Value.ToDateTime(TimeOnly.MinValue) : null;
        set => _pFrom = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    private DateTime? DonutFilterToDateTime
    {
        get => _pTo.HasValue ? _pTo.Value.ToDateTime(TimeOnly.MinValue) : null;
        set => _pTo = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    private void RebuildDonutMonthOptions()
    {
        var months = _allItems
            .Select(t => new DateOnly(t.OccurredOn.Year, t.OccurredOn.Month, 1))
            .Distinct()
            .OrderByDescending(m => m)
            .ToList();

        // Always include current month
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        if (!months.Contains(currentMonth))
            months.Insert(0, currentMonth);

        _pMonthOptions = months;
    }

    private void InitDonutDefaults()
    {
        if (_pMonthInitDone)
            return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        _pMonth = new DateOnly(today.Year, today.Month, 1);
        _pMonthInitDone = true;
    }

    private void ClearDonutDates()
    {
        _pMonth = null;
        _pFrom = null;
        _pTo = null;
        ApplyDonutFilters();
    }

    private void ResetDonutFilters()
    {
        _pFrom = null;
        _pTo = null;
        _pAccountId = null;

        var today = DateOnly.FromDateTime(DateTime.Today);
        _pMonth = new DateOnly(today.Year, today.Month, 1);

        ApplyDonutFilters();
    }

    private void ApplyDonutFilters()
    {
        IEnumerable<TransactionDto> q = _allItems;

        // month overrides from/to
        if (_pMonth is not null)
        {
            _pFrom = new DateOnly(_pMonth.Value.Year, _pMonth.Value.Month, 1);
            _pTo = _pFrom.Value.AddMonths(1).AddDays(-1);
        }

        if (_pFrom is not null)
            q = q.Where(x => x.OccurredOn >= _pFrom.Value);

        if (_pTo is not null)
            q = q.Where(x => x.OccurredOn <= _pTo.Value);

        if (_pAccountId is not null)
            q = q.Where(x => x.AccountId == _pAccountId.Value);

        q = q.Where(x => !x.IsPlanned);
        q = q.Where(x => x.EntryType == EntryType.Outcome);

        RebuildDonutChart(q);
    }

    private void RebuildDonutChart(IEnumerable<TransactionDto> filtered)
    {
        var groups = filtered
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Sum = g.Sum(x => x.Amount)
            })
            .Where(x => x.Sum > 0)
            .OrderByDescending(x => x.Sum)
            .ToList();

        var palette = _donutOptions.ChartPalette;

        _donutLegend = groups.Select((x, i) => new DonutLegendRow(
            Label: CategoryName(x.CategoryId),
            Value: (double)x.Sum,
            Color: palette[i % palette.Length]
        )).ToList();

        _donutLabels = _donutLegend.Select(r => $"{r.Label} ({r.Value})").ToArray();
        _donutSeries = new List<ChartSeries<double>>
        {
            new ChartSeries<double>
            {
                Data = _donutLegend.Select(r => r.Value).ToArray<double>()
            }
        };
    }

    private string CategoryName(Guid id)
        => _categoryById.TryGetValue(id, out var c) ? c.Name : "Category";

    // ---------------------------
    // LINE helpers
    // ---------------------------
    private void InitLineMonths()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        // Show last 24 months in selector, default select last 12
        _lineMonthOptions = Enumerable.Range(0, 24)
            .Select(i => currentMonth.AddMonths(-i))
            .ToList();

        _lineSelectedMonths = _lineMonthOptions
            .Take(12)
            .ToHashSet();
    }

    private Task OnLineMonthsChanged(IEnumerable<DateOnly> values)
    {
        _lineSelectedMonths = values?.ToHashSet() ?? new HashSet<DateOnly>();
        RebuildLineChart();
        return Task.CompletedTask;
    }

    private Task OnLineTypesChanged(IEnumerable<EntryType> values)
    {
        _lineSelectedTypes = values?.ToHashSet() ?? new HashSet<EntryType>();

        // Prevent empty selection
        if (_lineSelectedTypes.Count == 0)
            _lineSelectedTypes.Add(EntryType.Outcome);

        RebuildLineCategoryOptions();
        RebuildLineChart();
        return Task.CompletedTask;
    }

    private Task OnLineCategoriesChanged(IEnumerable<Guid> values)
    {
        _lineSelectedCategoryIds = values?.ToHashSet() ?? new HashSet<Guid>();
        RebuildLineChart();
        return Task.CompletedTask;
    }

    private void ClearLineCategories()
    {
        _lineSelectedCategoryIds.Clear();
        RebuildLineChart();
    }

    private void ResetLineFilters()
    {
        InitLineMonths();
        _lineSelectedTypes = new HashSet<EntryType> { EntryType.Outcome };
        _lineSelectedCategoryIds.Clear();

        RebuildLineCategoryOptions();
        RebuildLineChart();
    }

    private void RebuildLineCategoryOptions()
    {
        var allowedCategoryIds = _allItems
            .Where(t => _lineSelectedTypes.Contains(t.EntryType))
            .Select(t => t.CategoryId)
            .Distinct()
            .ToHashSet();

        _lineCategoryOptions = _categories
            .Where(c => allowedCategoryIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToList();

        // Drop invalid selections
        _lineSelectedCategoryIds.IntersectWith(allowedCategoryIds);
    }

    private void RebuildLineChart()
    {
        var months = _lineSelectedMonths
            .OrderBy(m => m)
            .ToList();

        _lineXAxisLabels = months
            .Select(m => m.ToString("MMM yyyy"))
            .ToArray();

        if (months.Count == 0)
        {
            _lineSeries = new List<ChartSeries<double>>();
            return;
        }

        var baseTx = _allItems.Where(t => _lineSelectedTypes.Contains(t.EntryType));

        // If categories are selected -> one line per category
        if (_lineSelectedCategoryIds.Count > 0)
        {
            var series = new List<ChartSeries<double>>();

            foreach (var catId in _lineSelectedCategoryIds)
            {
                var byMonth = baseTx
                    .Where(t => t.CategoryId == catId)
                    .GroupBy(t => new DateOnly(t.OccurredOn.Year, t.OccurredOn.Month, 1))
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

                var data = months.Select(m => byMonth.TryGetValue(m, out var v) ? (double)v : 0d).ToArray();

                series.Add(new ChartSeries<double>
                {
                    Name = CategoryName(catId),
                    Data = data
                });
            }

            _lineSeries = series;
            return;
        }

        // Otherwise -> totals (one line per selected type)
        var totalSeries = new List<ChartSeries<double>>();
        foreach (var type in _lineSelectedTypes.OrderBy(t => t))
        {
            var byMonth = _allItems
                .Where(t => t.EntryType == type)
                .GroupBy(t => new DateOnly(t.OccurredOn.Year, t.OccurredOn.Month, 1))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var data = months.Select(m => byMonth.TryGetValue(m, out var v) ? (double)v : 0d).ToArray();

            totalSeries.Add(new ChartSeries<double>
            {
                Name = type.ToLocalizedString(L),
                Data = data
            });
        }

        _lineSeries = totalSeries;
    }
}
