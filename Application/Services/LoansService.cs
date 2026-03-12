using Application.Common;
using Domain.Contracts;
using Domain.Ports;

namespace Application.Services
{
    public sealed class LoansService(ILoansRepository loansRepo, ILoanPaymentsRepository paymentsRepo)
    {
        private readonly ILoansRepository _loansRepo = loansRepo;
        private readonly ILoanPaymentsRepository _paymentsRepo = paymentsRepo;

        public async Task<(List<LoanDto> Loans, List<LoanPaymentDto> Payments)> GetRawAsync()
        {
            var loans = (await _loansRepo.ListAsync()).ToList();
            var payments = (await _paymentsRepo.ListAsync()).ToList();

            return (loans, payments);
        }

        public async Task AddAsync(LoanDto loan, IReadOnlyList<LoanPaymentDto> paymentsToInsert)
        {
            var name = (loan.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new AppValidationException("Enter loan name");

            loan.Name = name;
            loan.Currency = NormalizeCurrency(loan.Currency);

            var insertedLoan = await _loansRepo.InsertReturningAsync(loan);

            foreach (var p in paymentsToInsert)
            {
                p.LoanId = insertedLoan.Id;
                p.UserId = insertedLoan.UserId;
                await _paymentsRepo.InsertAsync(p);
            }
        }

        public async Task RenameAsync(LoanDto loan, string newName)
        {
            var name = (newName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new AppValidationException("Enter loan name");

            var updated = new LoanDto
            {
                Id = loan.Id,
                UserId = loan.UserId,
                Name = name,

                Currency = loan.Currency,
                Amount = loan.Amount,
                PaymentsCount = loan.PaymentsCount,
                StartDate = loan.StartDate,
                HasInterest = loan.HasInterest,
                InterestRate = loan.InterestRate,
                Note = loan.Note,

                CreatedAt = loan.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _loansRepo.UpdateAsync(updated);
        }

        public async Task UpdateAsync(LoanDto loan,
                                      IReadOnlyList<LoanPaymentDto> paymentsToInsert,
                                      IReadOnlyList<LoanPaymentDto> paymentsToDelete)
        {
            var name = (loan.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new AppValidationException("Enter loan name");

            loan.Name = name;
            loan.Currency = NormalizeCurrency(loan.Currency);

            await _loansRepo.UpdateAsync(loan);

            foreach (var payment in paymentsToDelete)
                await _paymentsRepo.DeleteAsync(payment);

            foreach (var payment in paymentsToInsert)
            {
                payment.LoanId = loan.Id;
                payment.UserId = loan.UserId;
                await _paymentsRepo.InsertAsync(payment);
            }
        }

        public async Task UpdatePaymentAsync(LoanPaymentDto payment)
            => await _paymentsRepo.UpdateAsync(payment);

        public async Task DeleteAsync(LoanDto loan)
        {
            var payments = await _paymentsRepo.ListByLoanIdAsync(loan.Id);

            foreach (var p in payments)
                await _paymentsRepo.DeleteAsync(p);

            await _loansRepo.DeleteAsync(loan);
        }

        private static string NormalizeCurrency(string currency)
            => string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();
    }
}
