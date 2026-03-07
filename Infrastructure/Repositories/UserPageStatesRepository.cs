using Domain.Contracts;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public sealed class UserPageStatesRepository(DatabaseService db) : IUserPageStatesRepository
{
    private readonly DatabaseService _db = db;

    public async Task<UserPageStateDto?> GetByPageKeyAsync(string pageKey, CancellationToken ct = default)
    {
        var rows = await _db.From<UserPageState>(q => q.Eq("page_key", pageKey));
        return rows.FirstOrDefault()?.ToDto();
    }

    public async Task<UserPageStateDto> InsertReturningAsync(UserPageStateDto state, CancellationToken ct = default)
    {
        var inserted = (await _db.Insert(state.ToModel())).FirstOrDefault()
            ?? throw new InvalidOperationException("Insert user_page_states returned no row.");

        return inserted.ToDto();
    }

    public async Task<UserPageStateDto> UpdateReturningAsync(UserPageStateDto state, CancellationToken ct = default)
    {
        var updated = (await _db.Update(state.ToModel())).FirstOrDefault()
            ?? throw new InvalidOperationException("Update user_page_states returned no row.");

        return updated.ToDto();
    }

    public async Task DeleteByPageKeyAsync(string pageKey, CancellationToken ct = default)
    {
        var rows = await _db.From<UserPageState>(q => q.Eq("page_key", pageKey));
        var model = rows.FirstOrDefault();

        if (model is null)
            return;

        await _db.Delete(model);
    }
}