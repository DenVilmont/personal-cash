namespace Domain.Ports;

public interface ITransactionsLookup
{
    Task<bool> AnyForAccountAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> AnyForCategoryAsync(Guid categoryId, CancellationToken ct = default);
}