using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Pages.Reports.Components
{
    public partial class ReportsCategoryChart
    {
        [Parameter] public bool Busy { get; set; }
        [Parameter] public bool HasData { get; set; }
        [Parameter] public decimal Total { get; set; }
        [Parameter] public string Currency { get; set; } = string.Empty;
        [Parameter] public List<ChartSeries<double>> ChartSeries { get; set; } = new();
        [Parameter] public string[] ChartLabels { get; set; } = Array.Empty<string>();
        [Parameter] public ChartOptions ChartOptions { get; set; } = new();

        private string FormatAmount(decimal value)
            => string.IsNullOrWhiteSpace(Currency)
                ? value.ToString("N2")
                : $"{value:N2} {Currency}";

    }
}
