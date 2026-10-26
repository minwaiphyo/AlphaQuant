using System.Globalization;
using AlphaQuant.Core;

namespace AlphaQuant.Desktop.ViewModels;

/// <summary>Display formatting only. Values are rounded here, never in Core.</summary>
internal static class Display
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    public static string Money(decimal value, string unit) =>
        unit == "USD"
            ? value.ToString("C0", Culture)
            : $"{value.ToString("N0", Culture)} {unit}";

    public static string Metric(MetricResult metric) =>
        metric.Value is { } value
            ? $"{value.ToString("N2", Culture)}{metric.DisplayUnit}"
            : "n/a";
}
