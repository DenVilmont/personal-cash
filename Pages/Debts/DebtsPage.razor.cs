using Application.Services;
using Domain.Contracts;
using Infrastructure.Auth;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Pages.Debts;

[Authorize]
public partial class DebtsPage
{
    [Inject] private LoansService LoansService { get; set; } = default!;
    [Inject] private CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;

    private string _defaultCurrency = "EUR";

    private List<LoanDto> _loans = new();
    private List<LoanPaymentDto> _payments = new();
    private readonly Dictionary<Guid, List<LoanPaymentDto>> _paymentsByLoanId = new();

    protected override async Task OnInitializedAsync()
    {
        if (!CurrentUser.TryGetUserId(out _))
            return;

        var userSettings = await UserSettingsStore.GetAsync();
        if (userSettings is not null && !string.IsNullOrWhiteSpace(userSettings.PreferredCurrency))
            _defaultCurrency = userSettings.PreferredCurrency.Trim().ToUpperInvariant();

        await LoadAsync();
    }

    protected Task LoadAsync() => RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        var (loansRaw, payments) = await LoansService.GetRawAsync();
        _payments = payments;

        _paymentsByLoanId.Clear();
        foreach (var p in _payments)
        {
            if (!_paymentsByLoanId.TryGetValue(p.LoanId, out var list))
            {
                list = new List<LoanPaymentDto>();
                _paymentsByLoanId[p.LoanId] = list;
            }
            list.Add(p);
        }

        foreach (var kv in _paymentsByLoanId)
            kv.Value.Sort((a, b) => a.DueDate.CompareTo(b.DueDate));

        DateOnly FirstPaymentDate(LoanDto l)
        {
            var list = PaymentsFor(l.Id);
            return list.Count > 0 ? list.Min(x => x.DueDate) : l.StartDate;
        }

        _loans = loansRaw
            .OrderBy(l => IsFullyPaid(l) ? 1 : 0)              // paid -> end
            .ThenByDescending(l => FirstPaymentDate(l))        // newer first payment -> first
            .ThenByDescending(l => l.CreatedAt)
            .ToList();
    }

    private string LoanCardClass(LoanDto loan) => IsFullyPaid(loan) ? "loan-card loan-paid" : "loan-card loan-open";

    private bool IsFullyPaid(LoanDto loan)
    {
        var payments = PaymentsFor(loan.Id);
        return payments.Count > 0 && payments.All(p => p.IsPaid);
    }

    private IReadOnlyList<LoanPaymentDto> PaymentsFor(Guid loanId)
        => _paymentsByLoanId.TryGetValue(loanId, out var list) ? list : Array.Empty<LoanPaymentDto>();

    private LoanSummaryModel LoanSummary(LoanDto loan)
    {
        var list = PaymentsFor(loan.Id);
        var total = list.Sum(x => x.Amount);
        var remaining = list.Where(x => !x.IsPaid).Sum(x => x.Amount);
        var paidCount = list.Count(x => x.IsPaid);
        var totalCount = list.Count;

        var interestText = loan.HasInterest
            ? (loan.InterestRate == 0m ? L["Interest"].Value : $"{loan.InterestRate:0.##}%")
            : L["No interest"].Value;

        var meta = $"{L["Total"].Value}: {total:N2} {loan.Currency} · {L["Payments"].Value}: {totalCount} · {interestText} · {L["Paid"].Value}: {paidCount} / {totalCount}";

        return new LoanSummaryModel(remaining, meta);
    }

    private async Task OpenAddLoanAsync()
    {
        if (!CurrentUser.IsAuthenticated)
        {
            Snackbar.Add("Not authenticated", Severity.Error);
            return;
        }

        if (!CurrentUser.TryGetUserId(out var userId))
        {
            Snackbar.Add("Invalid user id", Severity.Error);
            return;
        }

        var parameters = new DialogParameters
        {
            ["UserId"] = userId,
            ["Currency"] = _defaultCurrency
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<EditLoanDialog>(L["Add loan"].Value, parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
            return;
        if (result.Data is not LoanEditorResult data)
            return;

        await RunAsync(async () =>
        {
            await LoansService.AddAsync(data.Loan, data.PaymentsToInsert);
            await LoadCoreAsync();
        }, successMessage: "Loan saved");
    }

    private async Task OpenRenameLoanAsync(LoanDto loan)
    {
        var parameters = new DialogParameters
        {
            ["Name"] = loan.Name
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<RenameLoanDialog>(L["Rename loan"].Value, parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;
        if (result.Data is not string newName)
            return;

        newName = newName.Trim();

        await RunAsync(async () =>
        {
            await LoansService.RenameAsync(loan, newName);
            await LoadCoreAsync();
        }, successMessage: "Updated");
    }

    private async Task OpenEditPaymentAsync(LoanDto loan, LoanPaymentDto payment)
    {
        var copy = new LoanPaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            LoanId = payment.LoanId,
            DueDate = payment.DueDate,
            Amount = payment.Amount,
            IsPaid = payment.IsPaid,
            Note = payment.Note,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };

        var parameters = new DialogParameters
        {
            ["Payment"] = copy,
            ["Currency"] = loan.Currency
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<EditLoanPaymentDialog>(L["Edit payment"].Value, parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
            return;
        if (result.Data is not LoanPaymentDto updated)
            return;

        await RunAsync(async () =>
        {
            await LoansService.UpdatePaymentAsync(updated);
            await LoadCoreAsync();
        }, successMessage: "Updated");
    }

    private async Task ConfirmDeleteLoanAsync(LoanDto loan)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var msg = (MarkupString)($"{L["Delete"].Value}: {loan.Name}?<br/><br/>{L["This cannot be undone."].Value}");
        var confirmed = await DialogService.ShowMessageBoxAsync(
            L["Delete loan?"].Value,
            msg,
            yesText: L["Delete"].Value,
            cancelText: L["Cancel"].Value,
            options: options);

        if (confirmed == true)
            await DeleteLoanAsync(loan);
    }

    private Task DeleteLoanAsync(LoanDto loan) 
        => RunAsync(async () =>
        {
            await LoansService.DeleteAsync(loan);
            await LoadCoreAsync();
        }, successMessage: "Deleted");

    private sealed record LoanSummaryModel(decimal Remaining, string MetaText);
}
