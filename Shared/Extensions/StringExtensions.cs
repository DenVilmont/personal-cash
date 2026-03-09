using System.Globalization;

namespace PersonalCash.Shared.Extensions
{
    public static class StringExtensions
    {
        public static bool TryParseDecimal(this string? text, out decimal value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalized = text
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty);

            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            normalized = normalized
                .Replace(",", decimalSeparator)
                .Replace(".", decimalSeparator);

            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out value);
        }
    }
}
