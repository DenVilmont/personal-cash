using Microsoft.AspNetCore.Components;
using MudBlazor;
using Domain.Contracts;

namespace PersonalCash.Pages.Debts;

public partial class EditLoanDialog
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public bool IsNew { get; set; }
    [Parameter] public Guid UserId { get; set; }
    [Parameter] public string Currency { get; set; } = "EUR";
    [Parameter] public LoanDto? Loan { get; set; }
    [Parameter] public IReadOnlyList<LoanPaymentDto>? Payments { get; set; }

    private string? _name;
    private string _currency = "EUR";
    private decimal _amount;
    private int _paymentsCount = 4;
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Today);
    private bool _hasInterest;
    private string? _note;

    private bool _amountLocked;
    private List<LoanPaymentPreview> _previewPayments = new();

    private DateTime? StartDatePicker
    {
        get => _startDate.ToDateTime(TimeOnly.MinValue);
        set => _startDate = value.HasValue
            ? DateOnly.FromDateTime(value.Value)
            : DateOnly.FromDateTime(DateTime.Today);
    }

    protected override void OnParametersSet()
    {
        _currency = string.IsNullOrWhiteSpace(Currency) ? "EUR" : Currency.Trim().ToUpperInvariant();

        if (Loan is not null)
        {
            _name = Loan.Name;
            _currency = string.IsNullOrWhiteSpace(Loan.Currency) ? _currency : Loan.Currency.Trim().ToUpperInvariant();
            _amount = Loan.Amount;
            _paymentsCount = Loan.PaymentsCount <= 0 ? 1 : Loan.PaymentsCount;
            _startDate = Loan.StartDate;
            _hasInterest = Loan.HasInterest;
            _note = Loan.Note;

            _amountLocked = Payments?.Any(payment => payment.IsPaid) == true;

            _previewPayments = (Payments ?? Array.Empty<LoanPaymentDto>())
                .OrderBy(payment => payment.DueDate)
                .Select(payment => new LoanPaymentPreview(payment.DueDate, payment.Amount))
                .ToList();
        }
        else
        {
            _name ??= "";
            _amount = 0m;
            _paymentsCount = 4;
            _startDate = DateOnly.FromDateTime(DateTime.Today);
            _hasInterest = false;
            _note = null;
            _amountLocked = false;
            RebuildPreview();
        }
    }

    private void Cancel()
        => MudDialog.Cancel();

    private void RebuildPreview()
    {
        if (Loan is not null)
            return;

        _previewPayments = BuildSchedulePreview(_amount, _paymentsCount, _startDate);
        StateHasChanged();
    }

    private static List<LoanPaymentPreview> BuildSchedulePreview(decimal amount, int paymentsCount, DateOnly startDate)
    {
        var list = new List<LoanPaymentPreview>();

        if (amount <= 0 || paymentsCount <= 0)
            return list;

        var baseAmount = Math.Round(amount / paymentsCount, 2, MidpointRounding.AwayFromZero);
        var sumBase = baseAmount * Math.Max(0, paymentsCount - 1);
        var lastAmount = Math.Round(amount - sumBase, 2, MidpointRounding.AwayFromZero);

        for (var index = 0; index < paymentsCount; index++)
        {
            var dueDate = startDate.AddMonths(index);
            var currentAmount = index == paymentsCount - 1 ? lastAmount : baseAmount;
            list.Add(new LoanPaymentPreview(dueDate, currentAmount));
        }

        return list;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_name))
            return;

        if (_amount <= 0 || _paymentsCount <= 0)
            return;

        if (Loan is null && UserId == Guid.Empty)
            return;

        if (Loan is null)
        {
            var loanId = Guid.NewGuid();

            var loan = new LoanDto
            {
                Id = loanId,
                UserId = UserId,
                Name = _name.Trim(),
                Currency = _currency,
                Amount = _amount,
                PaymentsCount = _paymentsCount,
                StartDate = _startDate,
                HasInterest = _hasInterest,
                Note = string.IsNullOrWhiteSpace(_note) ? null : _note.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var schedule = BuildSchedulePreview(_amount, _paymentsCount, _startDate)
                .Select(payment => new LoanPaymentDto
                {
                    Id = Guid.NewGuid(),
                    UserId = UserId,
                    LoanId = loanId,
                    DueDate = payment.DueDate,
                    Amount = payment.Amount,
                    IsPaid = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                })
                .ToList();

            MudDialog.Close(DialogResult.Ok(new LoanEditorResult(
                IsNew: true,
                Loan: loan,
                PaymentsToInsert: schedule,
                PaymentsToDelete: new List<LoanPaymentDto>())));

            return;
        }

        var updated = new LoanDto
        {
            Id = Loan.Id,
            UserId = Loan.UserId,
            Name = _name.Trim(),
            Currency = Loan.Currency,
            Amount = Loan.Amount,
            PaymentsCount = Loan.PaymentsCount,
            StartDate = Loan.StartDate,
            HasInterest = _hasInterest,
            InterestRate = Loan.InterestRate,
            Note = string.IsNullOrWhiteSpace(_note) ? null : _note.Trim(),
            CreatedAt = Loan.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var paymentsToDelete = new List<LoanPaymentDto>();
        var paymentsToInsert = new List<LoanPaymentDto>();

        if (!_amountLocked)
        {
            var needRebuildSchedule =
                _amount != Loan.Amount ||
                _paymentsCount != Loan.PaymentsCount ||
                _startDate != Loan.StartDate;

            if (needRebuildSchedule)
            {
                updated.Amount = _amount;
                updated.PaymentsCount = _paymentsCount;
                updated.StartDate = _startDate;

                paymentsToDelete = (Payments ?? Array.Empty<LoanPaymentDto>()).ToList();

                paymentsToInsert = BuildSchedulePreview(_amount, _paymentsCount, _startDate)
                    .Select(payment => new LoanPaymentDto
                    {
                        Id = Guid.NewGuid(),
                        UserId = Loan.UserId,
                        LoanId = Loan.Id,
                        DueDate = payment.DueDate,
                        Amount = payment.Amount,
                        IsPaid = false,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    })
                    .ToList();
            }
        }

        MudDialog.Close(DialogResult.Ok(new LoanEditorResult(
            IsNew: false,
            Loan: updated,
            PaymentsToInsert: paymentsToInsert,
            PaymentsToDelete: paymentsToDelete)));
    }

    private sealed record LoanPaymentPreview(DateOnly DueDate, decimal Amount);
}

public sealed record LoanEditorResult(
    bool IsNew,
    LoanDto Loan,
    List<LoanPaymentDto> PaymentsToInsert,
    List<LoanPaymentDto> PaymentsToDelete);