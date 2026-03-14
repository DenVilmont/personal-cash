using Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Pages.Debts.Components
{
    public partial class DebtsMobileList
    {
        [Parameter] public IReadOnlyList<LoanDto> Items { get; set; } = Array.Empty<LoanDto>();
        [Parameter] public IReadOnlyDictionary<Guid, List<LoanPaymentDto>> PaymentsByLoanId { get; set; }
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

        private IReadOnlyList<LoanPaymentDto> GetPaymentsByLoanId(Guid loanId)
            => PaymentsByLoanId.TryGetValue(loanId, out var payments)
                ? payments
                : Array.Empty<LoanPaymentDto>();

        private decimal RemainingAmount(LoanDto loan)
            => GetPaymentsByLoanId(loan.Id)
                .Where(x => !x.IsPaid)
                .Sum(x => x.Amount);

        private string PaymentsProgress(LoanDto loan)
        {
            var payments = GetPaymentsByLoanId(loan.Id);
            var paidCount = payments.Count(x => x.IsPaid);
            return $"{paidCount}/{payments.Count}";
        }

        private string NextOLastPaymentDateText(LoanDto loan)
        {
            var payments = GetPaymentsByLoanId(loan.Id);

            DateOnly date;
            if (payments.Count > 0)
            {
                var unpaid = payments.Where(x => !x.IsPaid).ToList();

                date = unpaid.Count > 0
                    ? unpaid.Min(x => x.DueDate)
                    : payments.Max(x => x.DueDate);
            }
            else
            {
                date = loan.StartDate;
            }
            return date.ToString("yyyy-MM-dd");
        }

        private string InterestText(LoanDto loan)
        {
            if (!loan.HasInterest)
                return L["Debts_NoInterest"];

            if (loan.InterestRate.HasValue && loan.InterestRate.Value > 0m)
                return $"{loan.InterestRate.Value:0.##}%";

            return L["Debts_WithInterest"];
        }

        private string IsFullyPaidClass(LoanDto loan)
        {
            if (GetPaymentsByLoanId(loan.Id).Any(x => x.IsPaid == false))
                return "debts-mobile-card-not_paid";

            return "debts-mobile-card-paid";
        }
    }
}
