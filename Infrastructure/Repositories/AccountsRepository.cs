using Domain.Contracts;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class AccountsRepository(DatabaseService db) : IAccountsRepository
{
    private readonly DatabaseService _db = db;

    public async Task<IReadOnlyList<AccountDto>> ListAsync(CancellationToken ct = default)
        => (await _db.From<Account>())
        .Select(x => x.ToDto())
        .ToList();

    public async Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        var model = await _db.Single<Account>(q => q.Eq("id", accountId));
        return model?.ToDto();
    }

    public async Task InsertAsync(AccountDto account, CancellationToken ct = default)
    {
        await _db.Insert(account.ToModel());
    }

    public async Task UpdateAsync(AccountDto account, CancellationToken ct = default)
    {
        await _db.Update(account.ToModel());
    }

    public async Task DeleteAsync(AccountDto account, CancellationToken ct = default)
    {
        await _db.Delete(account.ToModel());
    }
}