using Domain.Enums;

namespace PersonalCash.Presentation.Icons;

public static class AccountIconMudExtensions
{
    public static string GetIcon(this AccountIcon key) => key switch
    {
        AccountIcon.Wallet => MudBlazor.Icons.Material.Filled.AccountBalanceWallet,
        AccountIcon.Card => MudBlazor.Icons.Material.Filled.CreditCard,
        AccountIcon.Savings => MudBlazor.Icons.Material.Filled.Savings,
        AccountIcon.Cash => MudBlazor.Icons.Material.Filled.Payments,
        AccountIcon.Bank => MudBlazor.Icons.Material.Filled.AccountBalance,
        _ => MudBlazor.Icons.Material.Filled.HelpOutline
    };
}