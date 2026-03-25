using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Shared.Components;

public partial class PcDatePicker
{
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = default!;

    private MudDatePicker? _picker;
    private bool _isXs;
    private DateTime? _currentDate;

    [Parameter] public string? Label { get; set; }

    [Parameter] public DateTime? Date { get; set; }
    [Parameter] public EventCallback<DateTime?> DateChanged { get; set; }

    [Parameter] public Variant Variant { get; set; } = Variant.Outlined;
    [Parameter] public Margin Margin { get; set; } = Margin.Dense;
    [Parameter] public string? Class { get; set; } = "w-100";

    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Clearable { get; set; }

    [Parameter] public Origin AnchorOrigin { get; set; } = Origin.BottomRight;
    [Parameter] public Origin TransformOrigin { get; set; } = Origin.TopRight;

    [Parameter] public bool IsDialog { get; set; } = true;
    [Parameter] public bool IsShowActions { get; set; } = true;
    [Parameter] public bool IsModal { get; set; } = true;

    private PickerVariant ResolvedPickerVariant =>
        IsDialog && _isXs
            ? PickerVariant.Dialog
            : PickerVariant.Inline;

    private bool ResolvedModal =>
        IsModal
            ? _isXs
            : true;

    private bool ShowMobileActions =>
        IsShowActions && _isXs;

    protected override void OnParametersSet()
    {
        _currentDate = Date;
        Label = Label ?? L["Date"];
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