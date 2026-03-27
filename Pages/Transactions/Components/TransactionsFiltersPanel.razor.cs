using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace PersonalCash.Pages.Transactions.Components;

public partial class TransactionsFiltersPanel
{
    [Parameter] public bool IsCollapsible { get; set; } = true;

    [Parameter] public DateTime? FromDate { get; set; }
    [Parameter] public EventCallback<DateTime?> FromDateChanged { get; set; }

    [Parameter] public DateTime? ToDate { get; set; }
    [Parameter] public EventCallback<DateTime?> ToDateChanged { get; set; }

    [Parameter] public DateOnly? Month { get; set; }
    [Parameter] public EventCallback<DateOnly?> MonthChanged { get; set; }

    [Parameter] public IReadOnlyCollection<Guid> AccountIds { get; set; } = Array.Empty<Guid>();
    [Parameter] public EventCallback<IReadOnlyCollection<Guid>> AccountIdsChanged { get; set; }

    [Parameter] public IReadOnlyCollection<Guid> CategoryIds { get; set; } = Array.Empty<Guid>();
    [Parameter] public EventCallback<IReadOnlyCollection<Guid>> CategoryIdsChanged { get; set; }

    [Parameter] public EntryType? SelectedEntryType { get; set; }
    [Parameter] public EventCallback<EntryType?> SelectedEntryTypeChanged { get; set; }

    [Parameter] public bool? IsForPlanning { get; set; }
    [Parameter] public EventCallback<bool?> IsForPlanningChanged { get; set; }

    [Parameter] public string? Note { get; set; }
    [Parameter] public EventCallback<string?> NoteChanged { get; set; }

    [Parameter] public decimal? MinAmount { get; set; }
    [Parameter] public EventCallback<decimal?> MinAmountChanged { get; set; }

    [Parameter] public decimal? MaxAmount { get; set; }
    [Parameter] public EventCallback<decimal?> MaxAmountChanged { get; set; }

    [Parameter] public IReadOnlyList<DateOnly> MonthOptions { get; set; } = Array.Empty<DateOnly>();
    [Parameter] public IReadOnlyList<AccountDto> Accounts { get; set; } = Array.Empty<AccountDto>();
    [Parameter] public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();

    [Parameter] public EventCallback OnFiltersChanged { get; set; }
    [Parameter] public EventCallback OnClear {  get; set; }
    [Parameter] public EventCallback OnReset { get; set; }
    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnApply { get; set; }

    private async Task ApplyClicked()
    {
        await OnApply.InvokeAsync();
    }

    private static string FormatMonth(DateOnly? value)
    {
        return value.HasValue
            ? value.Value.ToString("yyyy MMMM", CultureInfo.CurrentCulture)
            : string.Empty;
    }

    private string GetAccountSelectionText(IReadOnlyList<string> selectedValues)
    {
        if (selectedValues.Count == 0)
            return L["Filter_Account_All"];

        var names = Accounts
            .Where(c => selectedValues.Contains(c.Id.ToString()))
            .Select(c => c.Name)
            .ToList();
        return string.Join(", ", names);
    }

    private string GetCategorySelectionText(IReadOnlyList<string> selectedValues)
    {
        if (selectedValues.Count == 0)
            return L["Filter_Category_All"];

        var names = Categories
            .Where(c => selectedValues.Contains(c.Id.ToString()))
            .Select(c => c.Name)
            .ToList();
        return string.Join(", ", names);
    }

    private async Task OnFromDateChanged(DateTime? value)
    {
        await FromDateChanged.InvokeAsync(value);
    }

    private async Task OnToDateChanged(DateTime? value)
    {
        await ToDateChanged.InvokeAsync(value);
    }

    private async Task OnMonthChanged(DateOnly? value)
    {
        await MonthChanged.InvokeAsync(value);
    }

    private async Task OnMonthCleared()
    {
        await MonthChanged.InvokeAsync(null);
    }

    private async Task OnAccountIdsChanged(IReadOnlyCollection<Guid> values)
    {
        await AccountIdsChanged.InvokeAsync(values);
    }

    private async Task OnEntryTypeChanged(EntryType? value)
    {
        await SelectedEntryTypeChanged.InvokeAsync(value);
    }

    private async Task OnCategoryIdsChanged(IReadOnlyCollection<Guid> values)
    {
        await CategoryIdsChanged.InvokeAsync(values);
    }

    private async Task OnIsForPlanningChanged(bool? value)
    {
        await IsForPlanningChanged.InvokeAsync(value);
    }

    private async Task OnNoteChanged(string? value)
    {
        await NoteChanged.InvokeAsync(value);
    }

    private async Task OnMinAmountChanged(decimal? value)
    {
        await MinAmountChanged.InvokeAsync(value);
    }

    private async Task OnMaxAmountChanged(decimal? value)
    {
        await MaxAmountChanged.InvokeAsync(value);
    }

    private Task OnClearClicked() => OnClear.InvokeAsync();
    private Task OnResetClicked() => OnReset.InvokeAsync();
    private Task OnSaveClicked() => OnSave.InvokeAsync();
}