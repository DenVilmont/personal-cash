using Microsoft.Extensions.Localization;

namespace PersonalCash.Shared.Extensions
{
    public static class EnumExtensions
    {
        public static string ToLocalizedString<TEnum>(this TEnum value, IStringLocalizer<SharedResources> l) 
            where TEnum : struct, Enum 
            => l[$"{typeof(TEnum).Name}.{value}"];
    }
}
