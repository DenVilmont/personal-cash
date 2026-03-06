using Domain.Contracts;

namespace Domain.Ports;

public interface IAccountsRepository
{
    Task<IReadOnlyList<AccountDto>> ListAsync(CancellationToken ct = default);

    Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default);

    Task InsertAsync(AccountDto account, CancellationToken ct = default);

    Task UpdateAsync(AccountDto account, CancellationToken ct = default);

    Task DeleteAsync(AccountDto account, CancellationToken ct = default);
}