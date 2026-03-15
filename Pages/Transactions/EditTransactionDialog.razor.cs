using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared.Extensions;
using System.Globalization;

namespace PersonalCash.Pages.Transactions
{
    public partial class EditTransactionDialog
    {

        [CascadingParameter] 
        public IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] 
        public TransactionDto Tx { get; set; } = default!;

        [Parameter]
        public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();

        [Parameter]
        public IReadOnlyList<AccountDto> Accounts { get; set; } = Array.Empty<AccountDto>();

        private DateOnly? _occurredOn;
        private string _amountText = string.Empty;
        private EntryType _entryType;
        private bool _isForPlanning;
        private string _currency = "";
        private Guid _accountId;
        private Guid _categoryId;
        private string? _note;

        protected override void OnInitialized()
        {
            _occurredOn = Tx.OccurredOn;
            _entryType = Tx.EntryType;
            _isForPlanning = Tx.IsPlanned;
            _currency = Tx.Currency;
            _amountText = Tx.Amount.ToString("0.00", CultureInfo.CurrentCulture);
            _accountId = Tx.AccountId;
            _categoryId = Tx.CategoryId;
            _note = Tx.Note;
        }

        private DateTime? OccurredOnPicker
        {
            get => _occurredOn?.ToDateTime(TimeOnly.MinValue);
            set => _occurredOn = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
        }

        private Task SaveAsync() 
            => RunAsync(() =>
            {
                if (_occurredOn is null)
                {
                    Snackbar.Add(L["Transaction_DateIsRequired_ValidationError"], Severity.Error);
                    return Task.CompletedTask;
                }

                if (!_amountText.TryParseDecimal(out var parsedAmount) || parsedAmount <= 0)
                {
                    Snackbar.Add(L["Transaction_AmountMustBeValidPositiveNumber_ValidationError"], Severity.Error);
                    return Task.CompletedTask;
                }

                Tx.OccurredOn = _occurredOn.Value;
                Tx.Amount = parsedAmount;
                Tx.EntryType = _entryType;
                Tx.IsPlanned = _isForPlanning;
                Tx.Currency = string.IsNullOrWhiteSpace(_currency) ? "EUR" : _currency.Trim().ToUpperInvariant();
                Tx.AccountId = _accountId;
                Tx.CategoryId = _categoryId;
                Tx.Note = _note;
                
                MudDialog.Close(DialogResult.Ok(Tx));
                return Task.CompletedTask;
            });

        private void Cancel() => MudDialog.Cancel();
    }
}
