namespace Domain.Contracts
{
    public sealed class LoanPaymentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid LoanId { get; set; }
        public DateOnly DueDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
