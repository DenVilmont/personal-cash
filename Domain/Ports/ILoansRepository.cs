using Domain.Contracts;

namespace Domain.Ports
{
    public interface ILoansRepository
    {
        Task<IReadOnlyList<LoanDto>> ListAsync(CancellationToken ct = default);
        Task<LoanDto> InsertReturningAsync(LoanDto loan, CancellationToken ct = default);
        Task UpdateAsync(LoanDto loan, CancellationToken ct = default);
        Task DeleteAsync(LoanDto loan, CancellationToken ct = default);
    }
}
