using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Shared.Components;

public partial class PcDateSelectionDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public DateTime? Date { get; set; }

    private DateTime? _currentDate;

    protected override void OnParametersSet()
    {
        _currentDate = Date;
    }

    private Task OnDateChanged(DateTime? value)
    {
        _currentDate = value;
        return Task.CompletedTask;
    }

    private void SetToday()
    {
        _currentDate = DateTime.Today;
        MudDialog.Close(DialogResult.Ok(_currentDate));
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private void Submit()
    {
        MudDialog.Close(DialogResult.Ok(_currentDate));
    }
}