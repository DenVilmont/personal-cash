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
            => ApplyGroupBalances(await _accountsRepo.ListAsync())
                .OrderBy(x => x.IsArchived)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();

        public async Task<List<AccountDto>> GetActiveAsync()
            => ApplyGroupBalances(await _accountsRepo.ListAsync())
                .Where(x => !x.IsArchived)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();

        public async Task<List<AccountDto>> GetActiveRegularAsync()
            => (await GetActiveAsync())
                .Where(x => x.AccountType == AccountType.Regular)
                .ToList();

        // Backward-compatible overload for current UI.
        public Task AddAsync(
            Guid userId,
            string name,
            string currency,
            bool showBalance,
            string iconKey,
            int sortOrder)
            => AddAsync(
                userId,
                name,
                currency,
                showBalance,
                iconKey,
                sortOrder,
                AccountType.Regular,
                null);

        public async Task AddAsync(
            Guid userId,
            string name,
            string currency,
            bool showBalance,
            string iconKey,
            int sortOrder,
            AccountType accountType,
            Guid? parentAccountId)
        {
            var normalizedName = (name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new AppValidationException("Name is required");

            var existing = await _accountsRepo.ListAsync();

            if (existing.Any(a => string.Equals(a.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
                throw new AppValidationException("Account already exists");

            var normalizedCurrency = NormalizeCurrency(currency);

            ValidateAccountTypeAndParent(
                accountId: null,
                accountType: accountType,
                parentAccountId: parentAccountId,
                currency: normalizedCurrency,
                existing: existing);

            var item = new AccountDto
            {
                UserId = userId,
                Name = normalizedName,
                ShowBalance = showBalance,
                Currency = normalizedCurrency,
                IconKey = NormalizeIconKey(iconKey),
                SortOrder = sortOrder,
                AccountType = accountType,
                ParentAccountId = accountType == AccountType.Regular ? parentAccountId : null,
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

            var current = existing.FirstOrDefault(x => x.Id == acc.Id)
                ?? throw new AppValidationException("Account not found");

            if (acc.AccountType != current.AccountType)
                throw new AppValidationException("Account type cannot be changed");

            acc.Name = normalizedName;
            acc.Currency = NormalizeCurrency(acc.Currency);
            acc.IconKey = NormalizeIconKey(acc.IconKey);
            acc.ParentAccountId = acc.AccountType == AccountType.Regular
                ? acc.ParentAccountId
                : null;

            ValidateAccountTypeAndParent(
                accountId: acc.Id,
                accountType: acc.AccountType,
                parentAccountId: acc.ParentAccountId,
                currency: acc.Currency,
                existing: existing);

            if (acc.AccountType == AccountType.Group)
            {
                if (acc.IsArchived && existing.Any(x => x.ParentAccountId == acc.Id))
                    throw new AppValidationException("Account group with child accounts cannot be archived");

                acc.BalanceActual = 0m;
                acc.BalanceExpected = 0m;
            }

            await _accountsRepo.UpdateAsync(acc);
        }

        public async Task DeleteAsync(AccountDto acc)
        {
            var existing = await _accountsRepo.ListAsync();

            if (acc.AccountType == AccountType.Group && existing.Any(x => x.ParentAccountId == acc.Id))
                throw new AppValidationException("Account group has child accounts. It can't be deleted.");

            if (await _txLookup.AnyForAccountAsync(acc.Id))
                throw new AppValidationException("Account has transactions. It can't be deleted.");

            await _accountsRepo.DeleteAsync(acc);
        }

        public async Task<bool> HasChildAccountsAsync(Guid accountId)
            => (await _accountsRepo.ListAsync()).Any(x => x.ParentAccountId == accountId);

        public Task<bool> HasTransactionsAsync(Guid accountId)
            => _txLookup.AnyForAccountAsync(accountId);

        public async Task ArchiveAsync(AccountDto acc)
        {
            if (acc.AccountType == AccountType.Group && await HasChildAccountsAsync(acc.Id))
                throw new AppValidationException("Account group with child accounts cannot be archived");

            acc.IsArchived = true;
            await _accountsRepo.UpdateAsync(acc);
        }

        private static string NormalizeCurrency(string currency)
            => string.IsNullOrWhiteSpace(currency)
                ? "EUR"
                : currency.Trim().ToUpperInvariant();

        private static string NormalizeIconKey(string iconKey)
            => string.IsNullOrWhiteSpace(iconKey)
                ? AccountIcon.Wallet.ToString()
                : iconKey.Trim();

        private static void ValidateAccountTypeAndParent(
            Guid? accountId,
            AccountType accountType,
            Guid? parentAccountId,
            string currency,
            IReadOnlyList<AccountDto> existing)
        {
            if (accountType == AccountType.Group)
            {
                if (parentAccountId is not null)
                    throw new AppValidationException("Account group cannot have a parent account");

                return;
            }

            if (parentAccountId is null)
                return;

            if (accountId == parentAccountId)
                throw new AppValidationException("Account cannot be its own parent");

            var parent = existing.FirstOrDefault(x => x.Id == parentAccountId.Value)
                ?? throw new AppValidationException("Parent account not found");

            if (parent.AccountType != AccountType.Group)
                throw new AppValidationException("Parent account must be an account group");

            if (parent.IsArchived)
                throw new AppValidationException("Parent account must be active");

            if (!string.Equals(parent.Currency, currency, StringComparison.OrdinalIgnoreCase))
                throw new AppValidationException("Parent account currency must match child account currency");
        }

        private static List<AccountDto> ApplyGroupBalances(IReadOnlyList<AccountDto> accounts)
        {
            var result = accounts.Select(CloneAccount).ToList();

            var childrenByParent = result
                .Where(x => x.AccountType == AccountType.Regular)
                .Where(x => x.ParentAccountId is not null)
                .GroupBy(x => x.ParentAccountId!.Value)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var group in result.Where(x => x.AccountType == AccountType.Group))
            {
                if (!childrenByParent.TryGetValue(group.Id, out var children))
                {
                    group.BalanceActual = 0m;
                    group.BalanceExpected = 0m;
                    continue;
                }

                group.BalanceActual = children.Sum(x => x.BalanceActual);
                group.BalanceExpected = children.Sum(x => x.BalanceExpected);
            }

            return result;
        }

        private static AccountDto CloneAccount(AccountDto x) => new()
        {
            Id = x.Id,
            UserId = x.UserId,
            Name = x.Name,
            Currency = x.Currency,
            IconKey = x.IconKey,
            AccountType = x.AccountType,
            ParentAccountId = x.ParentAccountId,
            BalanceActual = x.BalanceActual,
            BalanceExpected = x.BalanceExpected,
            SortOrder = x.SortOrder,
            IsArchived = x.IsArchived,
            ShowBalance = x.ShowBalance,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}