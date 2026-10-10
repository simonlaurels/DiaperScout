using System.Globalization;

namespace DiaperScout.Web.Services;

public enum WaistMeasurementUnit { Centimetres, Inches }

public class WaistMeasurementEditor
{
    private decimal? minimumCm;
    private decimal? maximumCm;
    public WaistMeasurementUnit WaistUnit { get; set; }
    public string WaistUnitLabel => WaistUnit == WaistMeasurementUnit.Inches ? "in" : "cm";
    private decimal Factor => WaistUnit == WaistMeasurementUnit.Inches ? 2.54m : 1m;

    // Keep the unrounded centimetre values when toggling units; round only for storage.
    public decimal? WaistMinimum
    {
        get => minimumCm.HasValue ? decimal.Round(minimumCm.Value / Factor, 2) : null;
        set => minimumCm = value * Factor;
    }
    public decimal? WaistMaximum
    {
        get => maximumCm.HasValue ? decimal.Round(maximumCm.Value / Factor, 2) : null;
        set => maximumCm = value * Factor;
    }
    public int? WaistMinimumCm
    {
        get => ToStoredCentimetres(minimumCm);
        set => minimumCm = value;
    }
    public int? WaistMaximumCm
    {
        get => ToStoredCentimetres(maximumCm);
        set => maximumCm = value;
    }

    public string? ValidateWaist()
    {
        if (minimumCm is < 0 || maximumCm is < 0)
            return "Waist measurements cannot be negative.";
        if (minimumCm > int.MaxValue || maximumCm > int.MaxValue)
            return "Waist measurements are too large.";
        if (minimumCm.HasValue && maximumCm.HasValue && minimumCm > maximumCm)
            return "Waist minimum cannot be greater than maximum.";
        return null;
    }

    private static int? ToStoredCentimetres(decimal? value) => value.HasValue
        ? checked((int)decimal.Round(value.Value, 0, MidpointRounding.AwayFromZero))
        : null;
}

public static class WaistMeasurementFormatting
{
    public static string FormatRange(int? minimum, int? maximum)
    {
        if (minimum is null && maximum is null) return "Size recorded";
        static string Inches(int cm) => (cm / 2.54m).ToString("0.#", CultureInfo.CurrentCulture);
        if (minimum.HasValue && maximum.HasValue)
            return $"{minimum}–{maximum} cm (≈{Inches(minimum.Value)}–{Inches(maximum.Value)} in)";
        if (minimum.HasValue)
            return $"{minimum}+ cm (≈{Inches(minimum.Value)}+ in)";
        return $"Up to {maximum} cm (≈{Inches(maximum!.Value)} in)";
    }
}
