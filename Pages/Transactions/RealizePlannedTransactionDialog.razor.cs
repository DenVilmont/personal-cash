using Application.Services;
using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

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
        private decimal? _amount;
        private EntryType _entryType;
        private bool _isForPlanning;
        private string _currency = "";
        private Guid _accountId;
        private Guid _categoryId;
        private string? _note;

        private Task OnAmountChanged(decimal? value)
        {
            _amount = value;
            return Task.CompletedTask;
        }

        protected override void OnInitialized()
        {
            _occurredOn = DateOnly.FromDateTime(DateTime.Now);
            _entryType = Tx.EntryType;
            _isForPlanning = Tx.IsPlanned;
            _currency = Tx.Currency;
            _amount = Tx.Amount;
            _accountId = Tx.AccountId;
            _categoryId = Tx.CategoryId;
            _note = Tx.Note;
        }

        private IReadOnlyList<AccountDto> AccountOptions =>
            Accounts
            .Where(x => x.AccountType == AccountType.Regular)
            .Where(x => !x.IsArchived || x.Id == _accountId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)

        .ToList();
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

            if (_amount is null || _amount <= 0)
            {
                Snackbar.Add(L["Transaction_AmountMustBeValidPositiveNumber_ValidationError"], Severity.Error);
                return;
            }

            if (_accountId == Guid.Empty || !AccountOptions.Any(x => x.Id == _accountId))
            {
                Snackbar.Add(L["Transaction_AccountRequired_ValidationError"], Severity.Error);
                return;
            }


            if (_amount >= Tx.Amount)
            {
                await RunAsync(async () =>
                {
                    Tx.OccurredOn = _occurredOn.Value;
                    Tx.Amount = _amount.Value;
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
                    Amount = _amount.Value,
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

                    Tx.Amount -= _amount.Value;
                    Tx.IsPlanned = true;

                    MudDialog.Close(DialogResult.Ok(Tx));
                });
            }
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
