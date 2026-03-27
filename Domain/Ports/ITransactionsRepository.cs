using Domain.Contracts;
using Domain.Contracts.FiltersState;

namespace Domain.Ports
{
    public interface ITransactionsRepository
    {
        Task<IReadOnlyList<TransactionDto>> ListAsync(CancellationToken ct = default);
        Task<IReadOnlyList<TransactionDto>> ListAsync(TransactionsFilterStateDto filter, CancellationToken ct = default);

        Task<TransactionDto> InsertReturningAsync(TransactionDto tx, CancellationToken ct = default);

        Task UpdateAsync(TransactionDto tx, CancellationToken ct = default);

        Task DeleteAsync(TransactionDto tx, CancellationToken ct = default);
    }
}
