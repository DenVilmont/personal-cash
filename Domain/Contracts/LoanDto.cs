namespace Domain.Contracts
{
    public sealed class LoanDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public decimal Amount { get; set; }
        public int PaymentsCount { get; set; }
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public bool HasInterest {  get; set; }
        public decimal? InterestRate { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
