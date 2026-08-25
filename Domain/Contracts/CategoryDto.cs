namespace Domain.Contracts
{
    public sealed class CategoryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsTransferCategory { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

    }
}
