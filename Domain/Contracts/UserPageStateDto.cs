namespace Domain.Contracts
{
    public sealed class UserPageStateDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string PageKey { get; set; } = string.Empty;
        public string StateJson { get; set; } = "{}";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
