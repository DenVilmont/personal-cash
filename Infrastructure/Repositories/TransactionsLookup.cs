using Domain.Ports;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class TransactionsLookup(DatabaseService db) : ITransactionsLookup
{
    private readonly DatabaseService _db = db;

    public async Task<bool> AnyForAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        var items = await _db.From<Transaction>(q =>
            q.Eq("account_id", accountId)
             .Limit(1));

        return items.Count > 0;
    }

    public async Task<bool> AnyForCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        var items = await _db.From<Transaction>(q =>
            q.Eq("category_id", categoryId)
            .Limit(1));
        return items.Count > 0;
    }
}