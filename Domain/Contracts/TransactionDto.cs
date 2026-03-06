using Domain.Enums;

namespace Domain.Contracts
{
    public sealed class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public DateOnly OccurredOn { get; set; }
        public decimal Amount { get; set; }
        public EntryType EntryType { get; set; }
        public bool IsPlanned { get; set; }
        public string Currency { get; set; } = "EUR";
        public Guid CategoryId { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
