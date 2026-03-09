using Application.Common;
using Domain.Contracts;
using Domain.Enums;
using Domain.Ports;

namespace Application.Services
{
    public class AccountsService(IAccountsRepository accountsRepo, ITransactionsLookup txLookup)
    {
        private readonly IAccountsRepository _accountsRepo = accountsRepo;
        private readonly ITransactionsLookup _txLookup = txLookup;

        public async Task<List<AccountDto>> GetSortedAsync()
            => (await _accountsRepo.ListAsync())
            .OrderBy(x => x.IsArchived)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        public async Task<List<AccountDto>> GetActiveAsync()
            => (await _accountsRepo.ListAsync())
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();


        public async Task AddAsync(Guid userId, string name, string currency, bool showBalance, string iconKey, int sortOrder)
        {
            var normalizedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new AppValidationException("Name is required");

            var existing = await _accountsRepo.ListAsync();
            if (existing.Any(a => string.Equals(a.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new AppValidationException("Account already exists");
            }


            var item = new AccountDto
            {
                UserId = userId,
                Name = (name ?? string.Empty).Trim(),
                ShowBalance = showBalance,
                Currency = NormalizeCurrency(currency),
                IconKey = NormalizeIconKey(iconKey),
                SortOrder = sortOrder,
                IsArchived = false,
                BalanceActual = 0m,
                BalanceExpected = 0m
            };

            await _accountsRepo.InsertAsync(item);
        }

        public async Task UpdateAsync(AccountDto acc)
        {
            var normalizedName = (acc.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new AppValidationException("Name is required");

            var existing = await _accountsRepo.ListAsync();
            if (existing.Any(a => a.Id != acc.Id && string.Equals(a.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
                throw new AppValidationException("Account already exists");

            acc.Name = normalizedName;
            acc.Currency = NormalizeCurrency(acc.Currency);
            acc.IconKey = NormalizeIconKey(acc.IconKey);

            await _accountsRepo.UpdateAsync(acc);
        }

        public async Task DeleteAsync(AccountDto acc)
        {
            if (await _txLookup.AnyForAccountAsync(acc.Id))
                throw new AppValidationException("Account has transactions. It can't be deleted.");

            await _accountsRepo.DeleteAsync(acc);
        }

        public Task<bool> HasTransactionsAsync(Guid accountId)
            => _txLookup.AnyForAccountAsync(accountId);

        public async Task ArchiveAsync(AccountDto acc)
        {
            acc.IsArchived = true;
            await _accountsRepo.UpdateAsync(acc);
        }

        private static string NormalizeCurrency(string currency)
            => string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();

        private static string NormalizeIconKey(string iconKey)
            => string.IsNullOrWhiteSpace(iconKey) ? AccountIcon.Wallet.ToString() : iconKey.Trim();
    }
}
