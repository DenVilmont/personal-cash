using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace PersonalCash.Pages.Transactions.Components
{
    public partial class TransactionsBalancesPanel
    {
        [Parameter] public IReadOnlyList<AccountDto>? Accounts { get; set; }

        private IReadOnlyList<AccountDto> VisibleRootAccounts =>
            Accounts?
                .Where(x => x.ShowBalance)
                .Where(x =>
                    x.AccountType == AccountType.Group ||
                    x is { AccountType: AccountType.Regular, ParentAccountId: null })
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList()
            ?? new List<AccountDto>();

        private IReadOnlyList<AccountDto> GetChildAccounts(Guid groupAccountId)
            => Accounts?
                .Where(x => x.ShowBalance)
                .Where(x => x.AccountType == AccountType.Regular)
                .Where(x => x.ParentAccountId == groupAccountId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList()
            ?? new List<AccountDto>();

        private decimal GetBalanceActual(AccountDto account)
        {
            if (account.AccountType != AccountType.Group)
                return account.BalanceActual;

            return GetChildAccounts(account.Id).Sum(x => x.BalanceActual);
        }

        private decimal GetBalanceExpected(AccountDto account)
        {
            if (account.AccountType != AccountType.Group)
                return account.BalanceExpected;

            return GetChildAccounts(account.Id).Sum(x => x.BalanceExpected);
        }
    }
}