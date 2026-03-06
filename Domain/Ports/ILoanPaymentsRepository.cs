using Domain.Contracts;

namespace Domain.Ports
{
    public interface ILoanPaymentsRepository
    {
        Task<IReadOnlyList<LoanPaymentDto>> ListAsync(CancellationToken ct = default);
        Task<IReadOnlyList<LoanPaymentDto>> ListByLoanIdAsync(Guid loanId, CancellationToken ct = default);
        Task InsertAsync(LoanPaymentDto payment, CancellationToken ct = default);
        Task UpdateAsync(LoanPaymentDto payment, CancellationToken ct = default);
        Task DeleteAsync(LoanPaymentDto payment, CancellationToken ct = default);
    }
}
