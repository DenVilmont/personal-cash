using System.Net;
using Application.Services;
using Domain.Contracts;
using Infrastructure.Auth;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PersonalCash.Pages.Debts.State;
using PersonalCash.Shared;

namespace PersonalCash.Pages.Debts;

[Authorize]
public partial class DebtsPage : IDisposable
{
    [Inject] private LoansService LoansService { get; set; } = default!;
    [Inject] private CurrentUserService CurrentUser { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private UserSettingsStore UserSettingsStore { get; set; } = default!;
    [Inject] private DebtsUiRestoreService DebtsUiRestoreService { get; set; } = default!;
    [Inject] private AppPageTitleState PageTitleState { get; set; } = default!;

    private string _defaultCurrency = "EUR";

    private List<LoanDto> _loans = new();
    private readonly Dictionary<Guid, List<LoanPaymentDto>> _paymentsByLoanId = new();
    private HashSet<Guid> _expandedLoanIds = new();

    protected override void OnParametersSet()
    {
        PageTitleState.Set(L["Debts_PageTitle"]);
    }

    protected override async Task OnInitializedAsync()
    {
        if (!CurrentUser.TryGetUserId(out _))
            return;

        var userSettings = await UserSettingsStore.GetAsync();
        if (userSettings is not null && !string.IsNullOrWhiteSpace(userSettings.PreferredCurrency))
            _defaultCurrency = userSettings.PreferredCurrency.Trim().ToUpperInvariant();

        await LoadAsync();
    }

    protected Task LoadAsync()
        => RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        var (loansRaw, payments) = await LoansService.GetRawAsync();

        RebuildPaymentsIndex(payments);

        _loans = loansRaw
            .OrderBy(loan => IsFullyPaid(loan) ? 1 : 0)
            .ThenByDescending(loan => FirstPaymentDate(loan))
            .ThenByDescending(loan => loan.CreatedAt)
            .ToList();

        await TryApplyTransientRestoreAsync();
    }

    private void RebuildPaymentsIndex(IEnumerable<LoanPaymentDto> payments)
    {
        _paymentsByLoanId.Clear();

        foreach (var payment in payments)
        {
            if (!_paymentsByLoanId.TryGetValue(payment.LoanId, out var list))
            {
                list = new List<LoanPaymentDto>();
                _paymentsByLoanId[payment.LoanId] = list;
            }

            list.Add(payment);
        }

        foreach (var pair in _paymentsByLoanId)
            pair.Value.Sort((left, right) => left.DueDate.CompareTo(right.DueDate));
    }

    private Task ToggleLoanAsync(LoanDto loan)
    {
        if (!_expandedLoanIds.Add(loan.Id))
            _expandedLoanIds.Remove(loan.Id);

        return Task.CompletedTask;
    }

    private IReadOnlyList<LoanPaymentDto> PaymentsFor(Guid loanId)
        => _paymentsByLoanId.TryGetValue(loanId, out var list)
            ? list
            : Array.Empty<LoanPaymentDto>();

    private bool IsFullyPaid(LoanDto loan)
    {
        var payments = PaymentsFor(loan.Id);
        return payments.Count > 0 && payments.All(payment => payment.IsPaid);
    }

    private DateOnly FirstPaymentDate(LoanDto loan)
    {
        var payments = PaymentsFor(loan.Id);
        return payments.Count > 0 ? payments.Min(payment => payment.DueDate) : loan.StartDate;
    }

    private async Task OpenAddLoanAsync()
    {
        if (!CurrentUser.IsAuthenticated)
        {
            Snackbar.Add(L["NotAuthenticated_Error"], Severity.Error);
            return;
        }

        if (!CurrentUser.TryGetUserId(out var userId))
        {
            Snackbar.Add(L["InvalidUserId_Error"], Severity.Error);
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

        var dialog = await DialogService.ShowAsync<EditLoanDialog>(
            L["Debts_AddDebt_Title"].Value,
            parameters,
            options);

        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;

        if (result.Data is not LoanEditorResult data)
            return;

        await RunAsync(async () =>
        {
            await LoansService.AddAsync(data.Loan, data.PaymentsToInsert);
            await LoadCoreAsync();
        }, successMessage: L["Added"]);
    }

    private async Task OpenEditLoanAsync(LoanDto loan)
    {
        var loanCopy = CloneLoan(loan);
        var paymentCopies = PaymentsFor(loan.Id)
            .Select(ClonePayment)
            .ToList();

        var parameters = new DialogParameters
        {
            ["Loan"] = loanCopy,
            ["Payments"] = paymentCopies,
            ["UserId"] = loan.UserId,
            ["Currency"] = loan.Currency
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<EditLoanDialog>(
            L["Debts_EditDebt_Title"].Value,
            parameters,
            options);

        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;

        if (result.Data is not LoanEditorResult data)
            return;

        await RunAsync(async () =>
        {
            await PersistExpandedLoansForRestoreAsync();
            await LoansService.UpdateAsync(data.Loan, data.PaymentsToInsert, data.PaymentsToDelete);
            await LoadCoreAsync();
        }, successMessage: L["Updated"]);
    }

    private async Task OpenEditPaymentAsync((LoanDto Loan, LoanPaymentDto Payment) args)
    {
        var loan = args.Loan;
        var paymentCopy = ClonePayment(args.Payment);

        var parameters = new DialogParameters
        {
            ["Payment"] = paymentCopy,
            ["Currency"] = loan.Currency
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<EditLoanPaymentDialog>(
            L["Debts_EditPayment_Title"].Value,
            parameters,
            options);

        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;

        if (result.Data is not LoanPaymentDto updatedPayment)
            return;

        await RunAsync(async () =>
        {
            await PersistExpandedLoansForRestoreAsync();
            await LoansService.UpdatePaymentAsync(updatedPayment);
            await LoadCoreAsync();
        }, successMessage: L["Updated"]);
    }

    private async Task ConfirmDeleteLoanAsync(LoanDto loan)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var encodedName = WebUtility.HtmlEncode(loan.Name);
        var message = (MarkupString)string.Format(L["Debts_DeleteDialog_Message"].Value, encodedName);

        var confirmed = await DialogService.ShowMessageBoxAsync(
            L["Debts_DeleteDialog_Title"],
            message,
            yesText: L["Delete"],
            cancelText: L["Cancel"],
            options: options);

        if (confirmed == true)
            await DeleteLoanAsync(loan);
    }

    private Task DeleteLoanAsync(LoanDto loan)
        => RunAsync(async () =>
        {
            await PersistExpandedLoansForRestoreAsync();
            await LoansService.DeleteAsync(loan);
            await LoadCoreAsync();
        }, successMessage: L["Deleted"]);

    private async Task PersistExpandedLoansForRestoreAsync()
    {
        if (_expandedLoanIds.Count == 0)
        {
            await DebtsUiRestoreService.ClearAsync();
            return;
        }

        var state = new DebtsUiRestoreState
        {
            ExpandedLoanIds = _expandedLoanIds.ToList()
        };

        await DebtsUiRestoreService.SaveAsync(state);
    }

    private async Task TryApplyTransientRestoreAsync()
    {
        var state = await DebtsUiRestoreService.GetAsync();

        if (state is null || state.ExpandedLoanIds.Count == 0)
            return;

        var existingLoanIds = _loans
            .Select(loan => loan.Id)
            .ToHashSet();

        _expandedLoanIds = state.ExpandedLoanIds
            .Where(existingLoanIds.Contains)
            .ToHashSet();

        await DebtsUiRestoreService.ClearAsync();
    }

    private static LoanDto CloneLoan(LoanDto loan)
        => new()
        {
            Id = loan.Id,
            UserId = loan.UserId,
            Name = loan.Name,
            Currency = loan.Currency,
            Amount = loan.Amount,
            PaymentsCount = loan.PaymentsCount,
            StartDate = loan.StartDate,
            HasInterest = loan.HasInterest,
            InterestRate = loan.InterestRate,
            Note = loan.Note,
            CreatedAt = loan.CreatedAt,
            UpdatedAt = loan.UpdatedAt
        };

    private static LoanPaymentDto ClonePayment(LoanPaymentDto payment)
        => new()
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

    public void Dispose()
    {
        PageTitleState.Clear();
    }
}