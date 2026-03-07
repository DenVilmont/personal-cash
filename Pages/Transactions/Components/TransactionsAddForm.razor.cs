using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace PersonalCash.Pages.Transactions.Components;

public partial class TransactionsAddForm
{
    [Parameter] public DateTime? OccurredOn { get; set; }
    [Parameter] public EventCallback<DateTime?> OccurredOnChanged { get; set; }

    [Parameter] public decimal Amount { get; set; }
    [Parameter] public EventCallback<decimal> AmountChanged { get; set; }

    [Parameter] public EntryType EntryType { get; set; }
    [Parameter] public EventCallback<EntryType> EntryTypeChanged { get; set; }

    [Parameter] public bool IsForPlanning { get; set; }
    [Parameter] public EventCallback<bool> IsForPlanningChanged { get; set; }

    [Parameter] public string Currency { get; set; } = "EUR";

    [Parameter] public IReadOnlyList<AccountDto> Accounts { get; set; } = Array.Empty<AccountDto>();
    [Parameter] public Guid AccountId { get; set; }
    [Parameter] public EventCallback<Guid> AccountIdChanged { get; set; }

    [Parameter] public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    [Parameter] public Guid CategoryId { get; set; }
    [Parameter] public EventCallback<Guid> CategoryIdChanged { get; set; }

    [Parameter] public string? Note { get; set; }
    [Parameter] public EventCallback<string?> NoteChanged { get; set; }

    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnAdd { get; set; }
    [Parameter] public EventCallback OnRefresh { get; set; }

    private Task OnOccurredOnChanged(DateTime? value) => OccurredOnChanged.InvokeAsync(value);
    private Task OnAmountChanged(decimal value) => AmountChanged.InvokeAsync(value);
    private Task OnEntryTypeChanged(EntryType value) => EntryTypeChanged.InvokeAsync(value);
    private Task OnIsForPlanningChanged(bool value) => IsForPlanningChanged.InvokeAsync(value);
    private Task OnAccountIdChanged(Guid value) => AccountIdChanged.InvokeAsync(value);
    private Task OnCategoryIdChanged(Guid value) => CategoryIdChanged.InvokeAsync(value);
    private Task OnNoteChanged(string? value) => NoteChanged.InvokeAsync(value);

    private Task OnAddClicked() => OnAdd.InvokeAsync();
    private Task OnRefreshClicked() => OnRefresh.InvokeAsync();
}