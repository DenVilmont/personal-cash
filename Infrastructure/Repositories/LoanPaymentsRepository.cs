using Domain.Contracts;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public sealed class LoanPaymentsRepository(DatabaseService db) : ILoanPaymentsRepository
    {
        private readonly DatabaseService _db = db;

        public async Task<IReadOnlyList<LoanPaymentDto>> ListAsync(CancellationToken ct = default)
            => (await _db.From<LoanPayment>())
            .Select(x => x.ToDto())
            .OrderBy(x => x.DueDate)
            .ToList();
      

        public async Task<IReadOnlyList<LoanPaymentDto>> ListByLoanIdAsync(Guid loanId, CancellationToken ct = default)
        {
            var models = await _db.From<LoanPayment>(q =>
                q.Eq("loan_id", loanId));

            return models.Select(x => x.ToDto()).ToList();
        }

        public async Task InsertAsync(LoanPaymentDto payment, CancellationToken ct = default)
            => await _db.Insert(payment.ToModel());

        public async Task UpdateAsync(LoanPaymentDto payment, CancellationToken ct = default)
            => await _db.Update(payment.ToModel());

        public async Task DeleteAsync(LoanPaymentDto payment, CancellationToken ct = default)
            => await _db.Delete(payment.ToModel());
    }
}