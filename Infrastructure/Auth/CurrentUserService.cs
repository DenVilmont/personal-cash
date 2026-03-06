namespace Infrastructure.Auth
{
    public sealed class CurrentUserService(Supabase.Client supabase)
    {
        private readonly Supabase.Client _supabase = supabase;

        public bool IsAuthenticated => _supabase.Auth.CurrentUser is not null;

        public string? Email => _supabase.Auth.CurrentUser?.Email;

        public bool TryGetUserId(out Guid userId)
        {
            userId = default;

            var id = _supabase.Auth.CurrentUser?.Id;
            return !string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out userId);
        }

        public Guid GetUserIdOrThrow()
        {
            if (!TryGetUserId(out var userId))
                throw new InvalidOperationException("Not authenticated");

            return userId;
        }
    }
}
