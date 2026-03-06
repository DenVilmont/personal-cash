using Domain.Contracts;
using Infrastructure.Models;

namespace Infrastructure.Mapping
{
    public static class LoanPaymentMapping
    {
        public static LoanPaymentDto ToDto(this LoanPayment m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            LoanId = m.LoanId,
            DueDate = m.DueDate,
            Amount = m.Amount,
            IsPaid = m.IsPaid,
            Note = m.Note,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };

        public static LoanPayment ToModel(this LoanPaymentDto d)
        {
            var now = DateTimeOffset.UtcNow;

            return new LoanPayment
            {
                Id = d.Id,
                UserId = d.UserId,
                LoanId = d.LoanId,
                DueDate = d.DueDate,
                Amount = d.Amount,
                IsPaid = d.IsPaid,
                Note = d.Note,
                CreatedAt = d.CreatedAt == default ? now : d.CreatedAt,
                UpdatedAt = d.UpdatedAt == default ? now : d.UpdatedAt
            };
        }
    }
}
