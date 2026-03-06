using Microsoft.AspNetCore.Components;
using MudBlazor;
using Domain.Contracts;

namespace PersonalCash.Pages.Debts;

public partial class EditLoanPaymentDialog
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public LoanPaymentDto Payment { get; set; } = default!;
    [Parameter] public string Currency { get; set; } = "EUR";

    private string _currency = "EUR";
    private DateOnly _dueDate;
    private decimal _amount;
    private bool _isPaid;
    private string? _note;

    protected override void OnParametersSet()
    {
        _currency = string.IsNullOrWhiteSpace(Currency) ? "EUR" : Currency.Trim().ToUpperInvariant();
        _dueDate = Payment.DueDate;
        _amount = Payment.Amount;
        _isPaid = Payment.IsPaid;
        _note = Payment.Note;
    }

    private DateTime? DueDatePicker
    {
        get => _dueDate.ToDateTime(TimeOnly.MinValue);
        set => _dueDate = value.HasValue ? DateOnly.FromDateTime(value.Value) : _dueDate;
    }

    private void Cancel() => MudDialog.Cancel();

    private Task SaveAsync()
            => RunAsync(() =>
            {
                if (_amount < 0)
                    return Task.CompletedTask;

                Payment.DueDate = _dueDate;
                Payment.Amount = _amount;
                Payment.IsPaid = _isPaid;
                Payment.Note = string.IsNullOrWhiteSpace(_note) ? null : _note.Trim();

                MudDialog.Close(DialogResult.Ok(Payment));
                return Task.CompletedTask;
            });
    /*
    private void Save()
    {
        if (_amount < 0)
            return;

        Payment.DueDate = _dueDate;
        Payment.Amount = _amount;
        Payment.IsPaid = _isPaid;
        Payment.Note = string.IsNullOrWhiteSpace(_note) ? null : _note.Trim();

        MudDialog.Close(DialogResult.Ok(Payment));
    }
    */
}
