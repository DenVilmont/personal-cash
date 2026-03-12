using Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Pages.Debts.Components
{
    public partial class DebtsTable
    {
        [Parameter] public IReadOnlyList<LoanDto> Items { get; set; } = Array.Empty<LoanDto>();
        [Parameter]
        public IReadOnlyDictionary<Guid, List<LoanPaymentDto>> PaymentsByLoanId { get; set; }
            = new Dictionary<Guid, List<LoanPaymentDto>>();
        [Parameter] public IReadOnlyCollection<Guid> ExpandedLoanIds { get; set; } = Array.Empty<Guid>();
        [Parameter] public bool Busy { get; set; }

        [Parameter] public EventCallback<LoanDto> OnToggleLoan { get; set; }
        [Parameter] public EventCallback<LoanDto> OnEditLoan { get; set; }
        [Parameter] public EventCallback<LoanDto> OnDeleteLoan { get; set; }
        [Parameter] public EventCallback<(LoanDto Loan, LoanPaymentDto Payment)> OnEditPayment { get; set; }

        private bool IsExpanded(Guid loanId)
            => ExpandedLoanIds.Contains(loanId);

        private string GetToggleIcon(Guid loanId)
            => IsExpanded(loanId)
                ? Icons.Material.Filled.KeyboardArrowDown
                : Icons.Material.Filled.KeyboardArrowRight;

        private IReadOnlyList<LoanPaymentDto> GetPayments(Guid loanId)
            => PaymentsByLoanId.TryGetValue(loanId, out var payments)
                ? payments
                : Array.Empty<LoanPaymentDto>();

        private decimal RemainingAmount(LoanDto loan)
            => GetPayments(loan.Id)
                .Where(x => !x.IsPaid)
                .Sum(x => x.Amount);

        private string PaymentsProgress(LoanDto loan)
        {
            var payments = GetPayments(loan.Id);
            var paidCount = payments.Count(x => x.IsPaid);
            return $"{paidCount}/{payments.Count}";
        }

        private string FirstPaymentDateText(LoanDto loan)
        {
            var payments = GetPayments(loan.Id);
            var firstDate = payments.Count > 0 ? payments.Min(x => x.DueDate) : loan.StartDate;
            return firstDate.ToString("yyyy-MM-dd");
        }

        private string InterestText(LoanDto loan)
        {
            if (!loan.HasInterest)
                return L["Debts_NoInterest"];

            if (loan.InterestRate.HasValue && loan.InterestRate.Value > 0m)
                return $"{loan.InterestRate.Value:0.##}%";

            return L["Debts_WithInterest"];
        }

        private Task HandleRowClick(TableRowClickEventArgs<LoanDto> args)
        {
            if (args.Item is null)
                return Task.CompletedTask;

            return OnToggleLoan.InvokeAsync(args.Item);
        }

        private string IsFullyPaidClass(LoanDto loan, int index)
        {
            if (GetPayments(loan.Id).Any(x => x.IsPaid == false))
                return "debts-table-row-not_paid";

            return "debts-table-row-paid";
        }
    }
}
