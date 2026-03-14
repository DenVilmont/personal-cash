using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace PersonalCash.Pages.Reports.Components
{
    public partial class ReportsSummaryPanel
    {
        [Parameter] public bool Busy { get; set; }
        [Parameter] public Dictionary<string, string> Income { get; set; } = default!;
        [Parameter] public Dictionary<string, string> Expenses { get; set; } = default!;
        [Parameter] public Dictionary<string, string> Balances { get; set; } = default!;

        private string MonthCaption => DateOnly.FromDateTime(DateTime.Today)
                .ToString("MMMM yyyy", CultureInfo.CurrentUICulture);

        private sealed record SummarySection(
            string TitleKey,
            IReadOnlyDictionary<string, string>? Items);

        private IReadOnlyList<SummarySection> SummarySections =>
        [
            new(L["Reports_MonthSummary_Income"], Income),
            new(L["Reports_MonthSummary_Expenses"], Expenses),
            new(L["Reports_MonthSummary_CurrentBalances"], Balances)
        ];
    }
}
