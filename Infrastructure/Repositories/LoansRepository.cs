using Domain.Contracts;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public sealed class LoansRepository(DatabaseService db) : ILoansRepository
    {
        private readonly DatabaseService _db = db;

        public async Task<IReadOnlyList<LoanDto>> ListAsync(CancellationToken ct = default)
            => (await _db.From<Loan>())
            .Select(x => x.ToDto())
            .ToList();

        public async Task<LoanDto> InsertReturningAsync(LoanDto loan, CancellationToken ct = default)
        {
            var inserted = (await _db.Insert(loan.ToModel())).FirstOrDefault()
                ?? throw new InvalidOperationException("Insert loan returned no row.");
            return inserted.ToDto();
        }

        public async Task UpdateAsync(LoanDto loan, CancellationToken ct = default)
            => await _db.Update(loan.ToModel());

        public async Task DeleteAsync(LoanDto loan, CancellationToken ct = default)
            => await _db.Delete(loan.ToModel());
    }
}
