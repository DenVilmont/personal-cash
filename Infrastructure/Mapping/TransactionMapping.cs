using Domain.Contracts;
using Infrastructure.Models;

namespace Infrastructure.Mapping
{
    public static class TransactionMapping
    {
        public static TransactionDto ToDto(this Transaction m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            AccountId = m.AccountId,
            OccurredOn = m.OccurredOn,
            Amount = m.Amount,
            EntryType = m.EntryType,
            IsPlanned = m.IsPlanned,
            Currency = m.Currency,
            CategoryId = m.CategoryId,
            Note = m.Note,
            CreatedAt = m.CreatedAt
        };

        public static Transaction ToModel(this TransactionDto d)
        {
            var m = new Transaction
            {
                Id = d.Id,
                UserId = d.UserId,
                AccountId = d.AccountId,
                OccurredOn = d.OccurredOn,
                Amount = d.Amount,
                EntryType = d.EntryType,
                IsPlanned = d.IsPlanned,
                Currency = d.Currency,
                CategoryId = d.CategoryId,
                Note = d.Note
            };

            if (d.CreatedAt != default)
                m.CreatedAt = d.CreatedAt;

            return m;
        }
    }
}
