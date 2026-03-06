using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("loan_payments")]
public class LoanPayment : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("loan_id")]
    public Guid LoanId { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("is_paid")]
    public bool IsPaid { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
