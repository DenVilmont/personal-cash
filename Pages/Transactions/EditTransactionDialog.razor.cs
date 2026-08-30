using Domain.Contracts;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Shared.Components;

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
        private decimal? _amount;
        private EntryType _entryType;
        private bool _isForPlanning;
        private string _currency = "";
        private Guid _accountId;
        private Guid? _destinationAccountId;
        private Guid? _originalDestinationAccountId;
        private Guid? _lastRegularCategoryId;
        private Guid _categoryId;
        private string? _note;

        private IReadOnlyList<AccountDto> AccountOptions =>
            Accounts
                .Where(x => x.AccountType == AccountType.Regular)
                .Where(x => !x.IsArchived || x.Id == _accountId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();

        private CategoryDto? TransferCategory =>
            Categories.FirstOrDefault(x => x.IsTransferCategory);

        private IReadOnlyList<CategoryDto> CategoryOptions =>
            (_entryType == EntryType.Transfer
                ? Categories.Where(x => x.IsTransferCategory)
                : Categories.Where(x => !x.IsTransferCategory))
            .ToList();

        private IReadOnlyList<AccountDto> DestinationAccountOptions =>
            Accounts
                .Where(x => x.AccountType == AccountType.Regular)
                .Where(x => x.Id != _accountId)
                .Where(x => string.Equals(
                    x.Currency,
                    _currency,
                    StringComparison.OrdinalIgnoreCase))
                .Where(x =>
                    !x.IsArchived ||
                    x.Id == _originalDestinationAccountId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();

        private Task OnAmountChanged(decimal? value)
        {
            _amount = value;
            return Task.CompletedTask;
        }

        private Task OnAccountIdChanged(Guid value)
        {
            _accountId = value;

            var account = Accounts.FirstOrDefault(x => x.Id == value);

            if (account is not null &&
                !string.IsNullOrWhiteSpace(account.Currency))
            {
                _currency = account.Currency
                    .Trim()
                    .ToUpperInvariant();
            }

            if (_entryType == EntryType.Transfer &&
                _destinationAccountId is Guid destinationAccountId &&
                !DestinationAccountOptions.Any(x => x.Id == destinationAccountId))
            {
                _destinationAccountId = null;
            }

            return Task.CompletedTask;
        }

        private Task OnDestinationAccountIdChanged(Guid? value)
        {
            _destinationAccountId = value;
            return Task.CompletedTask;
        }

        private Task OnEntryTypeChanged(EntryType value)
        {
            if (_entryType == value)
                return Task.CompletedTask;

            var wasTransfer = _entryType == EntryType.Transfer;
            var becomesTransfer = value == EntryType.Transfer;

            if (!wasTransfer && becomesTransfer)
            {
                RememberRegularCategory();

                _entryType = EntryType.Transfer;
                _destinationAccountId = null;
                _categoryId = TransferCategory?.Id ?? Guid.Empty;

                return Task.CompletedTask;
            }

            _entryType = value;

            if (wasTransfer && !becomesTransfer)
            {
                _destinationAccountId = null;
                RestoreRegularCategory();
            }

            return Task.CompletedTask;
        }

        private void RememberRegularCategory()
        {
            if (_categoryId != Guid.Empty &&
                Categories.Any(x =>
                    x.Id == _categoryId &&
                    !x.IsTransferCategory))
            {
                _lastRegularCategoryId = _categoryId;
            }
        }

        private void RestoreRegularCategory()
        {
            if (_lastRegularCategoryId is Guid previousCategoryId &&
                Categories.Any(x =>
                    x.Id == previousCategoryId &&
                    !x.IsTransferCategory))
            {
                _categoryId = previousCategoryId;
                return;
            }

            _categoryId = Categories
                .FirstOrDefault(x => !x.IsTransferCategory)?
                .Id ?? Guid.Empty;

            _lastRegularCategoryId =
                _categoryId == Guid.Empty
                    ? null
                    : _categoryId;
        }

        protected override void OnInitialized()
        {
            _occurredOn = Tx.OccurredOn;
            _entryType = Tx.EntryType;
            _isForPlanning = Tx.IsPlanned;
            _currency = Tx.Currency;
            _amount = Tx.Amount;
            _accountId = Tx.AccountId;
            _destinationAccountId = Tx.DestinationAccountId;
            _originalDestinationAccountId = Tx.DestinationAccountId;
            _categoryId = Tx.CategoryId;
            _note = Tx.Note;

            if (_entryType == EntryType.Transfer)
            {
                _categoryId = TransferCategory?.Id ?? Guid.Empty;
            }
            else if (Categories.Any(x =>
                         x.Id == _categoryId &&
                         !x.IsTransferCategory))
            {
                _lastRegularCategoryId = _categoryId;
            }
            else
            {
                RestoreRegularCategory();
            }
        }

        private DateTime? OccurredOnPicker
        {
            get => _occurredOn?.ToDateTime(TimeOnly.MinValue);
            set => _occurredOn = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
        }

        private Task SaveAsync() => RunAsync(() =>
        {
            if (_occurredOn is null)
            {
                Snackbar.Add(
                    L["Transaction_DateRequired_ValidationError"],
                    Severity.Error);
                return Task.CompletedTask;
            }

            if (_amount is null || _amount <= 0)
            {
                Snackbar.Add(
                    L["Transaction_AmountMustBeValidPositiveNumber_ValidationError"],
                    Severity.Error);
                return Task.CompletedTask;
            }

            if (_accountId == Guid.Empty ||
                !AccountOptions.Any(x => x.Id == _accountId))
            {
                Snackbar.Add(
                    L["Transaction_AccountRequired_ValidationError"],
                    Severity.Error);
                return Task.CompletedTask;
            }

            if (_entryType == EntryType.Transfer)
            {
                if (_destinationAccountId is null ||
                    _destinationAccountId.Value == Guid.Empty)
                {
                    Snackbar.Add(
                        L["Transaction_DestinationAccountRequired_ValidationError"],
                        Severity.Error);
                    return Task.CompletedTask;
                }

                if (_destinationAccountId.Value == _accountId)
                {
                    Snackbar.Add(
                        L["Transaction_TransferAccountsMustDiffer_ValidationError"],
                        Severity.Error);
                    return Task.CompletedTask;
                }

                if (!DestinationAccountOptions.Any(
                        x => x.Id == _destinationAccountId.Value))
                {
                    Snackbar.Add(
                        L["Transaction_DestinationAccountInvalid_ValidationError"],
                        Severity.Error);
                    return Task.CompletedTask;
                }

                var transferCategory = TransferCategory;

                if (transferCategory is null ||
                    _categoryId != transferCategory.Id)
                {
                    Snackbar.Add(
                        L["Transaction_TransferCategoryRequired_ValidationError"],
                        Severity.Error);
                    return Task.CompletedTask;
                }
            }
            else
            {
                if (_categoryId == Guid.Empty ||
                    !Categories.Any(x =>
                        x.Id == _categoryId &&
                        !x.IsTransferCategory))
                {
                    Snackbar.Add(
                        L["Transaction_CategoryRequired_ValidationError"],
                        Severity.Error);
                    return Task.CompletedTask;
                }
            }

            Tx.OccurredOn = _occurredOn.Value;
            Tx.Amount = _amount.Value;
            Tx.EntryType = _entryType;
            Tx.IsPlanned = _isForPlanning;
            Tx.Currency = string.IsNullOrWhiteSpace(_currency)
                ? "EUR"
                : _currency.Trim().ToUpperInvariant();

            Tx.AccountId = _accountId;
            Tx.DestinationAccountId =
                _entryType == EntryType.Transfer
                    ? _destinationAccountId
                    : null;

            Tx.CategoryId = _categoryId;
            Tx.Note = _note;

            MudDialog.Close(DialogResult.Ok(Tx));
            return Task.CompletedTask;
        });

        private void Cancel() => MudDialog.Cancel();
    }
}