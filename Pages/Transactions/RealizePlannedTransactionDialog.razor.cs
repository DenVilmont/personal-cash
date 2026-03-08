using Application.Services;
using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared.Extensions;
using System.Globalization;

namespace PersonalCash.Pages.Transactions
{
    public partial class RealizePlannedTransactionDialog
    {
        [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public TransactionDto Tx { get; set; } = default!;
        [Parameter] public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
        [Parameter] public IReadOnlyList<AccountDto> Accounts { get; set; } = Array.Empty<AccountDto>();
        [Inject] private TransactionService TxService { get; set; } = default!;

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

        private async Task SaveAsync()
        {
            if (_occurredOn is null)
            {
                Snackbar.Add(L["Transaction_DateIsRequired_ValidationError"], Severity.Error);
                return;
            }

            if (!_amountText.TryParseDecimal(out var parsedAmount) || parsedAmount <= 0)
            {
                Snackbar.Add(L["Transaction_AmountMustBeValidPositiveNumber_ValidationError"], Severity.Error);
                return;
            }

            if (parsedAmount >= Tx.Amount)
            {
                await RunAsync(async () =>
                {
                    Tx.OccurredOn = _occurredOn.Value;
                    Tx.Amount = parsedAmount;
                    Tx.IsPlanned = false;
                    Tx.Note = _note;

                    MudDialog.Close(DialogResult.Ok(Tx));
                });
            }
            else
            {
                var copy = new TransactionDto
                {
                    OccurredOn = _occurredOn.Value,
                    Amount = parsedAmount,
                    EntryType = _entryType,
                    IsPlanned = false,
                    Currency = string.IsNullOrWhiteSpace(_currency) ? "EUR" : _currency.Trim().ToUpperInvariant(),
                    AccountId = _accountId,
                    CategoryId = _categoryId,
                    Note = _note,
                    UserId = Tx.UserId
                };

                await RunAsync(async () =>
                {
                    await TxService.InsertTransactionAndUpdateBalances(copy);

                    Tx.Amount -= parsedAmount;
                    Tx.IsPlanned = true;
                    Tx.Note = _note;

                    MudDialog.Close(DialogResult.Ok(Tx));
                });
            }
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
