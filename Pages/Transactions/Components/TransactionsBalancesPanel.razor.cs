using Domain.Contracts;
using Microsoft.AspNetCore.Components;

namespace PersonalCash.Pages.Transactions.Components
{
    public partial class TransactionsBalancesPanel
    {
        [Parameter] public IReadOnlyList<AccountDto>? Accounts { get; set; }

        private List<AccountDto> VisibleAccounts =>
            Accounts?
            .Where(x => x.ShowBalance)
            .ToList()
            ?? new List<AccountDto>();
    }
}
