namespace PersonalCash.Pages.Reports
{
    public sealed record ReportsCategoryRow(
    Guid CategoryId,
    string Label,
    double Amount,
    double Share,
    string Color);
}
