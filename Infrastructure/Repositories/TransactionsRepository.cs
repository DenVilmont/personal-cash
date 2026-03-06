using Domain.Contracts;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public sealed class TransactionsRepository(DatabaseService db) : ITransactionsRepository
    {
        private readonly DatabaseService _db = db;

        public async Task<IReadOnlyList<TransactionDto>> ListAsync(CancellationToken ct = default)
        {
            var models = await _db.From<Transaction>();
            return models.Select(x => x.ToDto()).ToList();
        }

        public async Task<TransactionDto> InsertReturningAsync(TransactionDto tx, CancellationToken ct = default)
        {
            var inserted = (await _db.Insert(tx.ToModel())).Single();
            return inserted.ToDto();
        }

        public async Task UpdateAsync(TransactionDto tx, CancellationToken ct = default)
        {
            await _db.Update(tx.ToModel());
        }

        public async Task DeleteAsync(TransactionDto tx, CancellationToken ct = default)
        {
            await _db.Delete(tx.ToModel());
        }
    }
}
