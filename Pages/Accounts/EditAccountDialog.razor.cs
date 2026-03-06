using Microsoft.AspNetCore.Components;
using MudBlazor;
using Domain.Enums;
using Domain.Contracts;

namespace PersonalCash.Pages.Accounts;

public partial class EditAccountDialog
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public AccountDto Account { get; set; } = default!;

    private string? _name;
    private string _currency = "EUR";
    protected bool _showBalance = true;
    private decimal _balanceActual = 0m;
    private AccountIcon _icon;
    private bool _isArchived;

    protected override void OnParametersSet()
    {
        _name = Account.Name;
        _currency = Account.Currency;
        _showBalance = Account.ShowBalance;
        _balanceActual = Account.BalanceActual;
        _icon = AccountIconExtensions.FromDbKey(Account.IconKey);
        _isArchived = Account.IsArchived;
    }

    private void Cancel() => MudDialog.Cancel();

    private Task SaveAsync()
        => RunAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(_name))
                return Task.CompletedTask;

            Account.Name = _name.Trim();
            Account.Currency = string.IsNullOrWhiteSpace(_currency) ? "EUR" : _currency.Trim().ToUpperInvariant();
            Account.ShowBalance = _showBalance;
            Account.IconKey = _icon.ToDbKey();
            Account.IsArchived = _isArchived;

            if (!Account.BalanceActual.Equals(_balanceActual))
            {
                var margin = _balanceActual - Account.BalanceActual;
                Account.BalanceActual += margin;
                Account.BalanceExpected += margin;
            }

            MudDialog.Close(DialogResult.Ok(Account));
            return Task.CompletedTask;
        });
}
