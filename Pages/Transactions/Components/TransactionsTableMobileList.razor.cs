using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace PersonalCash.Pages.Transactions.Components;

public partial class TransactionsTableMobileList
{
    [Parameter] public IReadOnlyList<TransactionDto> Items { get; set; } = Array.Empty<TransactionDto>();

    [Parameter]
    public IReadOnlyDictionary<Guid, AccountDto> AccountsById { get; set; }
        = new Dictionary<Guid, AccountDto>();

    [Parameter]
    public IReadOnlyDictionary<Guid, string> CategoriesById { get; set; }
        = new Dictionary<Guid, string>();

    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback<TransactionDto> OnEdit { get; set; }
    [Parameter] public EventCallback<TransactionDto> OnDelete { get; set; }
    [Parameter] public EventCallback<TransactionDto> OnRealizePlanned { get; set; }

    private decimal SignedAmount(TransactionDto tx)
        => tx.EntryType == EntryType.Outcome ? -tx.Amount : tx.Amount;

    private string GetAccountName(Guid id)
        => AccountsById.TryGetValue(id, out var account) ? account.Name : string.Empty;

    private string GetCategoryName(Guid id)
        => CategoriesById.TryGetValue(id, out var name) ? name : string.Empty;

    private AccountIcon GetAccountIcon(Guid id)
        => AccountIconExtensions.FromDbKey(
            AccountsById.TryGetValue(id, out var account) ? account.IconKey : null);

    private string GetCardClass(TransactionDto tx)
    {
        if (tx.IsPlanned)
            return "tx-mobile-planned";

        return tx.EntryType switch
        {
            EntryType.Income => "tx-mobile-income",
            EntryType.Outcome => "tx-mobile-outcome",
            _ => string.Empty
        };
    }
}