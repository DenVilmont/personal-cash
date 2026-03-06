using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("categories")]
public class Category : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
