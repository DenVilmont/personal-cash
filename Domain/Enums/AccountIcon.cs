namespace Domain.Enums
{
    public enum AccountIcon
    {
        Wallet,
        Card,
        Savings,
        Cash,
        Bank
    }

    public static class AccountIconExtensions
    {
        public static string ToDbKey(this AccountIcon key) => key.ToString();

        public static AccountIcon FromDbKey(string? dbKey)
            => Enum.TryParse<AccountIcon>(dbKey, ignoreCase: true, out var v) ? v : AccountIcon.Wallet;
    }
}