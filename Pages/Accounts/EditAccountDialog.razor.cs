using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared.Extensions;
using System.Globalization;

namespace PersonalCash.Pages.Accounts;

public partial class EditAccountDialog
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public AccountDto Account { get; set; } = default!;

    private string? _name;
    private string _currency = "EUR";
    protected bool _showBalance = true;
    private string _balanceActualText = string.Empty;
    private AccountIcon _icon;
    private bool _isArchived;

    protected override void OnParametersSet()
    {
        _name = Account.Name;
        _currency = Account.Currency;
        _showBalance = Account.ShowBalance;
        _balanceActualText = Account.BalanceActual.ToString("0.00", CultureInfo.CurrentCulture);
        _icon = AccountIconExtensions.FromDbKey(Account.IconKey);
        _isArchived = Account.IsArchived;
    }

    private void Cancel() => MudDialog.Cancel();

    private Task SaveAsync()
        => RunAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                Snackbar.Add(L["Accounts_NameRequired_ValidationError"], Severity.Warning);
                return Task.CompletedTask;
            }

            if (!_balanceActualText.TryParseDecimal(out var parsedBalanceActual))
            {
                Snackbar.Add(L["Accounts_BalanceActual_InvalidFormat_ValidationError"], Severity.Warning);
                return Task.CompletedTask;
            }

            Account.Name = _name.Trim();
            Account.Currency = string.IsNullOrWhiteSpace(_currency) ? "EUR" : _currency.Trim().ToUpperInvariant();
            Account.ShowBalance = _showBalance;
            Account.IconKey = _icon.ToDbKey();
            Account.IsArchived = _isArchived;

            if (!Account.BalanceActual.Equals(parsedBalanceActual))
            {
                var margin = parsedBalanceActual - Account.BalanceActual;
                Account.BalanceActual += margin;
                Account.BalanceExpected += margin;
            }

            MudDialog.Close(DialogResult.Ok(Account));
            return Task.CompletedTask;
        });
}
