namespace Domain.Contracts
{
    public sealed class UserSettingsDto
    {
        public Guid UserId { get; set; }
        public string? AvatarBase64 { get; set; }
        public string? AvatarMime { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string PreferredLanguage { get; set; } = "en";
        public string PreferredCurrency { get; set; } = "EUR";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
