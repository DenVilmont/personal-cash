using Application.Common;
using Domain.Contracts;
using Domain.Contracts.FiltersState;
using Domain.Enums;
using Domain.Ports;

namespace Application.Services
{
    public sealed class TransactionService(
        ITransactionsRepository txRepo,
        IAccountsRepository accountsRepo,
        ICategoriesRepository categoriesRepo)
    {
        private readonly ITransactionsRepository _txRepo = txRepo;
        private readonly IAccountsRepository _accountsRepo = accountsRepo;
        private readonly ICategoriesRepository _categoriesRepo = categoriesRepo;

        private readonly record struct AccountBalanceEffect(
            Guid AccountId,
            decimal ActualDelta,
            decimal ExpectedDelta);

        public async Task<List<TransactionDto>> GetFilteredAsync(
            TransactionsFilterStateDto filter)
            => (await _txRepo.ListAsync(filter)).ToList();

        public async Task<List<TransactionDto>> GetAllAsync()
            => (await _txRepo.ListAsync())
                .OrderByDescending(x => x.OccurredOn)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        public async Task<List<TransactionDto>> GetActualAsync()
            => (await _txRepo.ListAsync())
                .Where(x => x.IsPlanned == false)
                .OrderByDescending(x => x.OccurredOn)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        public async Task<List<TransactionDto>> GetPlannedAsync()
            => (await _txRepo.ListAsync())
                .Where(x => x.IsPlanned == true)
                .OrderByDescending(x => x.OccurredOn)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        private async Task ValidateTransactionForSaveAsync(TransactionDto tx)
        {

            if (tx.UserId == Guid.Empty)
                throw new AppValidationException("Invalid user id");

            if (tx.AccountId == Guid.Empty)
                throw new AppValidationException("Account is required");

            if (tx.Amount <= 0)
                throw new AppValidationException("Amount must be greater than 0");

            if (tx.CategoryId == Guid.Empty)
                throw new AppValidationException("Category is required");

            if (string.IsNullOrWhiteSpace(tx.Currency))
                throw new AppValidationException("Currency is required");

            var sourceAccount = await _accountsRepo.GetByIdAsync(tx.AccountId)
                ?? throw new AppValidationException("Account not found");

            ValidateRegularAccount(
                sourceAccount,
                "Transactions can use only regular accounts");

            if (sourceAccount.UserId != tx.UserId)
                throw new AppValidationException(
                    "Account does not belong to the transaction user");

            switch (tx.EntryType)
            {
                case EntryType.Income:
                case EntryType.Outcome:
                    {
                        if (tx.DestinationAccountId is not null)
                        {
                            throw new AppValidationException(
                                "Destination account is allowed only for transfers");
                        }

                        return;
                    }

                case EntryType.Transfer:
                    {
                        if (tx.IsPlanned)
                        {
                            throw new AppValidationException(
                                "Transfer transactions cannot be planned");
                        }

                        if (tx.DestinationAccountId is null ||
                            tx.DestinationAccountId == Guid.Empty)
                        {
                            throw new AppValidationException(
                                "Destination account is required for transfer");
                        }

                        var destinationAccountId = tx.DestinationAccountId.Value;

                        if (tx.AccountId == destinationAccountId)
                        {
                            throw new AppValidationException(
                                "Source and destination accounts must be different");
                        }

                        var destinationAccount =
                            await _accountsRepo.GetByIdAsync(destinationAccountId)
                            ?? throw new AppValidationException(
                                "Destination account not found");

                        ValidateRegularAccount(
                            destinationAccount,
                            "Transfers can use only regular accounts");

                        if (destinationAccount.UserId != tx.UserId)
                        {
                            throw new AppValidationException(
                                "Destination account does not belong to the transaction user");
                        }

                        if (!string.Equals(
                                sourceAccount.Currency,
                                destinationAccount.Currency,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new AppValidationException(
                                "Transfer accounts must use the same currency");
                        }

                        var category =
                            await _categoriesRepo.GetByIdAsync(tx.CategoryId)
                            ?? throw new AppValidationException(
                                "Category not found");

                        if (category.UserId != tx.UserId)
                        {
                            throw new AppValidationException(
                                "Category does not belong to the transaction user");
                        }

                        if (!category.IsTransferCategory)
                        {
                            throw new AppValidationException(
                                "Transfer must use the transfer category");
                        }

                        return;
                    }

                default:
                    throw new AppValidationException(
                        "Invalid transaction type");
            }
        }

        private static void ValidateRegularAccount(
            AccountDto account,
            string errorMessage)
        {
            if (account.AccountType != AccountType.Regular)
                throw new AppValidationException(errorMessage);
        }

        public async Task InsertTransactionAndUpdateBalances(TransactionDto tx)
        {
            await ValidateTransactionForSaveAsync(tx);

            var inserted = await _txRepo.InsertReturningAsync(tx);

            var effects = GetAccountEffects(inserted);

            try
            {
                await ApplyEffectsWithCompensationAsync(effects);
            }
            catch
            {
                try
                {
                    await _txRepo.DeleteAsync(inserted);
                }
                catch
                {
                    // Last-resort compensation, same principle as existing code.
                }

                throw;
            }
        }

        public async Task DeleteTransactionAndUpdateBalances(TransactionDto tx)
        {
            var reverseEffects = NegateEffects(GetAccountEffects(tx));

            await _txRepo.DeleteAsync(tx);

            try
            {
                await ApplyEffectsWithCompensationAsync(reverseEffects);
            }
            catch
            {
                // Balance compensation is already attempted inside
                // ApplyEffectsWithCompensationAsync.
                //
                // Restore the deleted transaction as the final compensation step.
                try
                {
                    await _txRepo.InsertReturningAsync(tx);
                }
                catch
                {
                    // Last resort, same compensation model as existing code.
                }

                throw;
            }
        }

        public async Task UpdateTransactionAndUpdateBalances(
            TransactionDto oldTx,
            TransactionDto newTx)
        {
            await ValidateTransactionForSaveAsync(newTx);

            var oldEffects = GetAccountEffects(oldTx);
            var newEffects = GetAccountEffects(newTx);

            var netEffects = BuildNetEffects(
                oldEffects,
                newEffects);

            await _txRepo.UpdateAsync(newTx);

            try
            {
                await ApplyEffectsWithCompensationAsync(netEffects);
            }
            catch
            {
                // Applied balance effects have already been compensated
                // as far as possible. Restore the old transaction state.
                try
                {
                    await _txRepo.UpdateAsync(oldTx);
                }
                catch
                {
                    // Last resort, same compensation model as existing code.
                }

                throw;
            }
        }

        private static IReadOnlyList<AccountBalanceEffect> GetAccountEffects(
            TransactionDto tx)
        {
            var actualAmount = tx.IsPlanned
                ? 0m
                : tx.Amount;

            var expectedAmount = tx.Amount;

            switch (tx.EntryType)
            {
                case EntryType.Income:
                    return new[]
                    {
                        new AccountBalanceEffect(
                            tx.AccountId,
                            actualAmount,
                            expectedAmount)
                    };

                case EntryType.Outcome:
                    return new[]
                    {
                        new AccountBalanceEffect(
                            tx.AccountId,
                            -actualAmount,
                            -expectedAmount)
                    };

                case EntryType.Transfer:
                    {
                        if (tx.DestinationAccountId is null ||
                            tx.DestinationAccountId == Guid.Empty)
                        {
                            throw new InvalidOperationException(
                                "Transfer transaction has no destination account.");
                        }

                        return new[]
                        {
                        new AccountBalanceEffect(
                            tx.AccountId,
                            -actualAmount,
                            -expectedAmount),

                        new AccountBalanceEffect(
                            tx.DestinationAccountId.Value,
                            actualAmount,
                            expectedAmount)
                    };
                    }

                default:
                    throw new InvalidOperationException(
                        $"Unsupported transaction type: {tx.EntryType}");
            }
        }

        private static IReadOnlyList<AccountBalanceEffect> NegateEffects(
            IReadOnlyList<AccountBalanceEffect> effects)
        {
            return effects
                .Select(effect => new AccountBalanceEffect(
                    effect.AccountId,
                    -effect.ActualDelta,
                    -effect.ExpectedDelta))
                .ToList();
        }

        private static IReadOnlyList<AccountBalanceEffect> BuildNetEffects(
            IReadOnlyList<AccountBalanceEffect> oldEffects,
            IReadOnlyList<AccountBalanceEffect> newEffects)
        {
            var totals =
                new Dictionary<Guid, (decimal Actual, decimal Expected)>();

            foreach (var effect in oldEffects)
            {
                AddToTotal(
                    totals,
                    effect.AccountId,
                    -effect.ActualDelta,
                    -effect.ExpectedDelta);
            }

            foreach (var effect in newEffects)
            {
                AddToTotal(
                    totals,
                    effect.AccountId,
                    effect.ActualDelta,
                    effect.ExpectedDelta);
            }

            return totals
                .Where(x =>
                    x.Value.Actual != 0m ||
                    x.Value.Expected != 0m)
                .Select(x => new AccountBalanceEffect(
                    x.Key,
                    x.Value.Actual,
                    x.Value.Expected))
                .ToList();
        }

        private static void AddToTotal(
            Dictionary<Guid, (decimal Actual, decimal Expected)> totals,
            Guid accountId,
            decimal actualDelta,
            decimal expectedDelta)
        {
            if (totals.TryGetValue(accountId, out var current))
            {
                totals[accountId] = (
                    current.Actual + actualDelta,
                    current.Expected + expectedDelta);
            }
            else
            {
                totals[accountId] = (
                    actualDelta,
                    expectedDelta);
            }
        }

        private async Task ApplyEffectsWithCompensationAsync(
            IReadOnlyList<AccountBalanceEffect> effects)
        {
            var appliedEffects =
                new List<AccountBalanceEffect>(effects.Count);

            try
            {
                foreach (var effect in effects)
                {
                    await ApplyAccountBalanceDelta(
                        effect.AccountId,
                        effect.ActualDelta,
                        effect.ExpectedDelta);

                    appliedEffects.Add(effect);
                }
            }
            catch
            {
                // Roll back only effects that completed successfully.
                // Reverse order makes the compensation symmetrical
                // with the original sequence.
                for (var i = appliedEffects.Count - 1; i >= 0; i--)
                {
                    var effect = appliedEffects[i];

                    try
                    {
                        await ApplyAccountBalanceDelta(
                            effect.AccountId,
                            -effect.ActualDelta,
                            -effect.ExpectedDelta);
                    }
                    catch
                    {
                        // Best effort, consistent with the existing
                        // compensation model.
                    }
                }

                throw;
            }
        }

        private async Task ApplyAccountBalanceDelta(
            Guid accountId,
            decimal actualDelta,
            decimal expectedDelta)
        {
            var acc = await _accountsRepo.GetByIdAsync(accountId);

            if (acc == null || !acc.ShowBalance)
                return;

            acc.BalanceActual += actualDelta;
            acc.BalanceExpected += expectedDelta;

            await _accountsRepo.UpdateAsync(acc);
        }

        public static decimal SignedAmount(
            decimal amount,
            EntryType entryType)
            => entryType == EntryType.Outcome
                ? -amount
                : amount;

        public static (
            decimal actualDelta,
            decimal expectedDelta) GetDeltas(TransactionDto tx)
        {
            if (tx.EntryType == EntryType.Transfer)
            {
                throw new InvalidOperationException(
                    "Transfer has multiple account balance effects.");
            }

            var signed = SignedAmount(
                tx.Amount,
                tx.EntryType);

            var expected = signed;
            var actual = tx.IsPlanned
                ? 0m
                : signed;

            return (actual, expected);
        }
    }
}