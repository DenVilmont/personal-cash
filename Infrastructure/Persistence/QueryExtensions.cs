using static Supabase.Postgrest.Constants;

namespace Infrastructure.Persistence
{
    public static class QueryExtensions
    {
        public static dynamic Eq(this object q, string column, string value)
            => ((dynamic)q).Filter(column, Operator.Equals, value);

        public static dynamic Eq(this object q, string column, Guid value)
            => ((dynamic)q).Filter(column, Operator.Equals, value.ToString());
    }
}
