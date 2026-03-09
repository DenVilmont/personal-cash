using Domain.Contracts;
using Infrastructure.Models;

namespace Infrastructure.Mapping;

public static class AccountMapping
{
    public static AccountDto ToDto(this Account m) => new()
    {
        Id = m.Id,
        UserId = m.UserId,
        Name = m.Name,
        Currency = m.Currency,
        IconKey = m.IconKey,
        SortOrder = m.SortOrder,
        BalanceActual = m.BalanceActual,
        BalanceExpected = m.BalanceExpected,
        IsArchived = m.IsArchived,
        ShowBalance = m.ShowBalance,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };

    public static Account ToModel(this AccountDto d)
    {
        var a = new Account
        {
            Id = d.Id,
            UserId = d.UserId,
            Name = d.Name,
            Currency = d.Currency,
            IconKey = d.IconKey,
            SortOrder = d.SortOrder,
            BalanceActual = d.BalanceActual,
            BalanceExpected = d.BalanceExpected,
            IsArchived = d.IsArchived,
            ShowBalance = d.ShowBalance,
            UpdatedAt = d.UpdatedAt == default ? DateTimeOffset.UtcNow : d.UpdatedAt,
        };
        if (d.CreatedAt != default)
            a.CreatedAt = d.CreatedAt;
        return a;
    }
}