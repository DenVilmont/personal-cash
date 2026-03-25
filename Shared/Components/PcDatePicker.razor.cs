using Microsoft.AspNetCore.Components;
using System.Globalization;
using MudBlazor;

namespace PersonalCash.Shared.Components;

public partial class PcDatePicker
{
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private MudDatePicker? _picker;
    private bool _isXs;
    private DateTime? _currentDate;

    [Parameter] public string? Label { get; set; }

    [Parameter] public DateTime? Date { get; set; }
    [Parameter] public EventCallback<DateTime?> DateChanged { get; set; }

    [Parameter] public Variant Variant { get; set; } = Variant.Outlined;
    [Parameter] public Margin Margin { get; set; } = Margin.None;
    [Parameter] public string? Class { get; set; } = "w-100";

    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Clearable { get; set; }

    [Parameter] public Origin AnchorOrigin { get; set; } = Origin.BottomRight;
    [Parameter] public Origin TransformOrigin { get; set; } = Origin.TopRight;

    [Parameter] public bool IsDialog { get; set; } = false;
    [Parameter] public bool IsShowActions { get; set; } = false;
    [Parameter] public bool IsModal { get; set; } = false;
    [Parameter] public bool UseStandaloneDialogOnXs { get; set; } = true;

    private string ResolvedLabel => Label ?? L["Date"];

    private bool UseStandaloneXsDialog => UseStandaloneDialogOnXs && _isXs;

    private string DisplayText =>
        _currentDate?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty;

    private async Task OpenStandaloneDialogAsync()
    {
        var parameters = new DialogParameters
        {
            ["Date"] = _currentDate
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<PcDateSelectionDialog>(
            ResolvedLabel,
            parameters,
            options);

        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;

        if (result.Data is DateTime date)
        {
            _currentDate = date;
            await DateChanged.InvokeAsync(date);
        }
    }

    private PickerVariant ResolvedPickerVariant =>
        IsDialog || _isXs
            ? PickerVariant.Dialog
            : PickerVariant.Inline;

    private bool ResolvedModal => IsModal || _isXs;

    private bool ShowMobileActions =>
        _isXs || IsShowActions;

    protected override void OnParametersSet()
    {
        _currentDate = Date;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var breakpoint = await BrowserViewportService.GetCurrentBreakpointAsync();
        var isXs = breakpoint == Breakpoint.Xs;

        if (_isXs == isXs)
            return;

        _isXs = isXs;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleDateChanged(DateTime? value)
    {
        _currentDate = value;
        await DateChanged.InvokeAsync(value);
    }

    private async Task SetTodayAsync()
    {
        var today = DateTime.Today;
        _currentDate = today;

        await DateChanged.InvokeAsync(today);

        if (_picker is null)
            return;

        await _picker.GoToDate(today, submitDate: true);
        await _picker.CloseAsync(true);
    }

    private async Task CancelAsync()
    {
        if (_picker is null)
            return;

        _currentDate = Date;
        await _picker.CloseAsync(false);
    }

    private async Task SubmitAsync()
    {
        if (_picker is null)
            return;

        await DateChanged.InvokeAsync(_currentDate);
        await _picker.CloseAsync(true);
    }
}