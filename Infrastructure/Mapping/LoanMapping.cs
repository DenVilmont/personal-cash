using Domain.Contracts;
using Infrastructure.Models;

namespace Infrastructure.Mapping
{
    public static class LoanMapping
    {
        public static LoanDto ToDto(this Loan m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Name = m.Name,
            Currency = m.Currency,
            Amount = m.Amount,
            PaymentsCount = m.PaymentsCount,
            StartDate = m.StartDate,
            HasInterest = m.HasInterest,
            InterestRate = m.InterestRate,
            Note = m.Note,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };

        public static Loan ToModel(this LoanDto d)
        {
            return new Loan
            {
                Id = d.Id,
                UserId = d.UserId,
                Name = d.Name,
                Currency = d.Currency,
                Amount = d.Amount,
                PaymentsCount = d.PaymentsCount,
                StartDate = d.StartDate,
                HasInterest = d.HasInterest,
                InterestRate = d.InterestRate,
                Note = d.Note,
                CreatedAt = d.CreatedAt == default ? DateTimeOffset.UtcNow : d.CreatedAt,
                UpdatedAt = d.UpdatedAt == default ? DateTimeOffset.UtcNow : d.UpdatedAt
            };
        }
    }
}
