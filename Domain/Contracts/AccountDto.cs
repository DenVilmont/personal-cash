namespace Domain.Contracts;

public sealed class AccountDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = "";

    public string Currency { get; set; } = "EUR";

    public string IconKey { get; set; } = "Wallet";

    public decimal BalanceActual { get; set; }

    public decimal BalanceExpected { get; set; }

    public bool IsArchived { get; set; }

    public bool ShowBalance { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}