using System.Text.Json;
using Domain.Contracts;
using Domain.Ports;

namespace Application.Services;

public sealed class PageStateService(IUserPageStatesRepository repo)
{
    private readonly IUserPageStatesRepository _repo = repo;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TState?> LoadAsync<TState>(string pageKey, CancellationToken ct = default)
        where TState : class
    {
        var row = await _repo.GetByPageKeyAsync(pageKey, ct);

        if (row is null || string.IsNullOrWhiteSpace(row.StateJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TState>(row.StateJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync<TState>(Guid userId, string pageKey, TState state, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);

        var json = JsonSerializer.Serialize(state, JsonOptions);
        var existing = await _repo.GetByPageKeyAsync(pageKey, ct);

        if (existing is null)
        {
            await _repo.InsertReturningAsync(new UserPageStateDto
            {
                UserId = userId,
                PageKey = pageKey,
                StateJson = json
            }, ct);

            return;
        }

        existing.UserId = userId;
        existing.PageKey = pageKey;
        existing.StateJson = json;

        await _repo.UpdateReturningAsync(existing, ct);
    }

    public Task DeleteAsync(string pageKey, CancellationToken ct = default)
        => _repo.DeleteByPageKeyAsync(pageKey, ct);
}