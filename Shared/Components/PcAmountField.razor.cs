using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace PersonalCash.Shared.Components;

public partial class PcAmountField
{
    private string _textValue = string.Empty;
    private decimal? _lastSyncedValue;

    [Parameter] public string? Label { get; set; }

    [Parameter] public decimal? Value { get; set; }
    [Parameter] public EventCallback<decimal?> ValueChanged { get; set; }

    [Parameter] public Variant Variant { get; set; } = Variant.Outlined;
    [Parameter] public Margin Margin { get; set; } = Margin.Dense;
    [Parameter] public string? Class { get; set; } = "w-100";

    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Immediate { get; set; } = true;
    [Parameter] public bool Clearable { get; set; }

    [Parameter] public int Decimals { get; set; } = 2;

    [Parameter] public string? Placeholder { get; set; }

    [Parameter] public Adornment Adornment { get; set; } = Adornment.None;
    [Parameter] public string? AdornmentText { get; set; }
    [Parameter] public string? AdornmentIcon { get; set; }
    [Parameter] public Color AdornmentColor { get; set; } = Color.Default;

    private string ResolvedPattern =  "[0-9,.]";

    protected override void OnParametersSet()
    {
        if (Value != _lastSyncedValue || string.IsNullOrWhiteSpace(_textValue))
        {
            _lastSyncedValue = Value;
            _textValue = FormatValue(Value);
        }
        Label = Label ?? @L["Amount"];
    }

    private async Task OnTextChanged(string? raw)
    {
        _textValue = raw ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_textValue))
        {
            _lastSyncedValue = null;
            await ValueChanged.InvokeAsync(null);
            return;
        }

        var normalized = Normalize(_textValue);

        if (!TryParseAmount(normalized, out var parsed))
            return;

        _lastSyncedValue = parsed;
        await ValueChanged.InvokeAsync(parsed);
    }

    private static string Normalize(string value)
    {
        var trimmed = value.Trim();

        return trimmed.Replace(',', '.');
    }

    private bool TryParseAmount(string value, out decimal parsed)
    {
        var style = NumberStyles.AllowDecimalPoint;

        return decimal.TryParse(value, style, CultureInfo.InvariantCulture, out parsed);
    }

    private string FormatValue(decimal? value)
    {
        if (value is null)
            return string.Empty;

        return Decimals switch
        {
            < 0 => value.Value.ToString(CultureInfo.InvariantCulture),
            0 => decimal.Truncate(value.Value).ToString(CultureInfo.InvariantCulture),
            _ => value.Value.ToString($"0.{new string('#', Decimals)}", CultureInfo.InvariantCulture)
        };
    }
}