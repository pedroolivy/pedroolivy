using System.Globalization;

namespace ProfileGenerator.Rendering;

public static class Num
{
    public static string Format(double value, int decimals = 1)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "a non-finite number cannot become SVG.");

        var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        return (rounded == 0 ? 0 : rounded).ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);
    }

    public static string Percent(double fraction) => Format(fraction * 100, 2) + "%";
}
