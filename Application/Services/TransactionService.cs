using Domain.Contracts;
using Domain.Enums;
using Domain.Ports;
using Application.Common;
using Domain.Contracts.FiltersState;

namespace Application.Services
{
    public sealed class TransactionService(ITransactionsRepository txRepo, IAccountsRepository accountsRepo)
    {
        private readonly ITransactionsRepository _txRepo = txRepo;
        private readonly IAccountsRepository _accountsRepo = accountsRepo;

        public async Task<List<TransactionDto>> GetFilteredAsync(TransactionsFilterStateDto filter)
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

        private static void ValidateTransactionForSave(TransactionDto tx)
        {
            if (tx.UserId == Guid.Empty)
                throw new AppValidationException("Invalid user id");

            if (tx.AccountId == Guid.Empty)
                throw new AppValidationException("Account is required");

            if (tx.CategoryId == Guid.Empty)
                throw new AppValidationException("Category is required");

            if (tx.Amount <= 0)
                throw new AppValidationException("Amount must be greater than 0");

            if (string.IsNullOrWhiteSpace(tx.Currency))
                throw new AppValidationException("Currency is required");
        }

        public async Task InsertTransactionAndUpdateBalances(TransactionDto tx)
        {
            ValidateTransactionForSave(tx);

            var inserted = await _txRepo.InsertReturningAsync(tx);

            try
            {
                var (actualDelta, expectedDelta) = GetDeltas(inserted);
                await ApplyAccountBalanceDelta(inserted.AccountId, actualDelta, expectedDelta);
            }
            catch
            {
                try { await _txRepo.DeleteAsync(inserted); } catch { /* last resort */ }
                throw;
            }
        }

        public async Task DeleteTransactionAndUpdateBalances(TransactionDto tx)
        {
            await _txRepo.DeleteAsync(tx);

            try
            {
                var (actualDelta, expectedDelta) = GetDeltas(tx);
                await ApplyAccountBalanceDelta(tx.AccountId, -actualDelta, -expectedDelta);
            }
            catch
            {
                // компенсация: удаление прошло, баланс — нет
                try { await _txRepo.InsertReturningAsync(tx); } catch { }
                throw;
            }
        }

        public async Task UpdateTransactionAndUpdateBalances(TransactionDto oldTx, TransactionDto newTx)
        {
            ValidateTransactionForSave(newTx);

            await _txRepo.UpdateAsync(newTx);

            var (oldActual, oldExpected) = GetDeltas(oldTx);
            var (newActual, newExpected) = GetDeltas(newTx);

            // Case 1: same account -> apply delta difference
            if (oldTx.AccountId == newTx.AccountId)
            {
                try
                {
                    await ApplyAccountBalanceDelta(newTx.AccountId, newActual - oldActual, newExpected - oldExpected);
                }
                catch
                {
                    // rollback tx update
                    try { await _txRepo.UpdateAsync(oldTx); } catch { }
                    throw;
                }

                return;
            }

            // Case 2: account changed -> remove old, then add new, with compensation
            try
            {
                // 1) remove old impact from old account
                await ApplyAccountBalanceDelta(oldTx.AccountId, -oldActual, -oldExpected);

                try
                {
                    // 2) add new impact to new account
                    await ApplyAccountBalanceDelta(newTx.AccountId, newActual, newExpected);
                }
                catch
                {
                    // compensation A: restore old account back
                    try { await ApplyAccountBalanceDelta(oldTx.AccountId, oldActual, oldExpected); } catch { }

                    // rollback tx update
                    try { await _txRepo.UpdateAsync(oldTx); } catch { }

                    throw;
                }
            }
            catch
            {
                // rollback tx update if first balance step failed
                try { await _txRepo.UpdateAsync(oldTx); } catch { }
                throw;
            }
        }


        private async Task ApplyAccountBalanceDelta(Guid accountId, decimal actualDelta, decimal expectedDelta)
        {
            var acc = await _accountsRepo.GetByIdAsync(accountId);

            if (acc == null || !acc.ShowBalance)
                return;

            acc.BalanceActual += actualDelta;
            acc.BalanceExpected += expectedDelta;

            await _accountsRepo.UpdateAsync(acc);
        }

        public static decimal SignedAmount(decimal amount, EntryType entryType)
        => entryType == EntryType.Outcome ? -amount : amount;

        public static (decimal actualDelta, decimal expectedDelta) GetDeltas(TransactionDto tx)
        {
            var signed = SignedAmount(tx.Amount, tx.EntryType);
            var expected = signed;
            var actual = tx.IsPlanned ? 0m : signed;
            return (actual, expected);
        }
    }
}
