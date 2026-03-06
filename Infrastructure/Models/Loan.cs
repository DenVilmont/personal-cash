using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("loans")]
public class Loan : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("currency")]
    public string Currency { get; set; } = "EUR";

    // Principal amount (without interest).
    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("payments_count")]
    public int PaymentsCount { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("has_interest")]
    public bool HasInterest { get; set; }

    // Optional percent value (e.g. 12.5 for 12.5%). Not applied automatically.
    [Column("interest_rate")]
    public decimal? InterestRate { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
