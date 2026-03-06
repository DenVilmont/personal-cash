using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("user_settings")]
public class UserSettings : BaseModel
{
    [PrimaryKey("user_id", false)]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("avatar_base64")]
    public string? AvatarBase64 { get; set; }

    [Column("avatar_mime")]
    public string? AvatarMime { get; set; }

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    [Column("preferred_language")]
    public string PreferredLanguage { get; set; } = "en";

    [Column("preferred_currency")]
    public string PreferredCurrency { get; set; } = "EUR";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
