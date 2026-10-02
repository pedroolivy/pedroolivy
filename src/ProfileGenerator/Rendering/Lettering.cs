using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProfileGenerator.Rendering;

public sealed record LetteringStyle(
    double X,
    double Baseline,
    double CapHeight,
    double? StrokeWidth = null);

public sealed record LetteringStroke(int Index, string PathData);

public sealed record LetteringPlacement(
    double OriginX,
    double OriginY,
    double Scale,
    double StrokeWidth,
    double Width,
    IReadOnlyList<LetteringStroke> Strokes)
{
    public string ToSvg(string? cssClass = null)
    {
        if (!string.IsNullOrEmpty(cssClass))
            Lettering.EnsureValidCssClassList(cssClass);

        var classes = string.IsNullOrEmpty(cssClass) ? "lettering" : $"lettering {cssClass}";
        var svg = new StringBuilder();
        svg.Append($"<g class=\"{classes}\" transform=\"translate({Num.Format(OriginX)} {Num.Format(OriginY)}) ")
           .Append($"scale({Num.Format(Scale, 4)})\" stroke-width=\"{Num.Format(StrokeWidth / Scale)}\">");
        foreach (var stroke in Strokes)
            svg.Append($"<path d=\"{stroke.PathData}\" pathLength=\"1\" class=\"ls\"/>");
        return svg.Append("</g>").ToString();
    }
}

public static partial class Lettering
{
    private const double StrokeToCapRatio = 0.07;

    private const char WordSpace = ' ';

    private static readonly FrozenDictionary<(char Left, char Right), double> PairKerning = new Dictionary<(char, char), double>
    {
        [('W', 'A')] = 24,
        [('A', 'W')] = 24,
        [('A', 'Y')] = 18,
        [('Y', 'A')] = 18,
        [('A', 'V')] = 28,
        [('V', 'A')] = 28,
        [('L', 'I')] = 12,
        [('L', 'V')] = 24,
        [('I', 'V')] = 12,
        [('V', 'E')] = 12,
        [('N', 'V')] = 12,
    }.ToFrozenDictionary();

    public static LetteringPlacement Place(string text, LetteringStyle style)
    {
        if (style.CapHeight <= 0)
            throw new RenderException("the lettering needs a cap height above zero.");
        if (style.StrokeWidth is <= 0)
            throw new RenderException("the lettering needs a stroke width above zero.");
        if (text.Length == 0)
            throw new RenderException("the lettering text is empty.");
        if (text[0] == WordSpace || text[^1] == WordSpace)
            throw new RenderException("the lettering text cannot start or end with a space.");

        var scale = style.CapHeight / GlyphTable.CapHeight;
        var placed = new List<string>();
        var cursor = 0.0;
        var textWidth = 0.0;
        char? previous = null;

        foreach (var character in text)
        {
            if (character == WordSpace)
            {
                cursor += GlyphTable.WordSpace - GlyphTable.Tracking;
                previous = null;
                continue;
            }

            if (!GlyphTable.Glyphs.TryGetValue(character, out var glyph))
                throw new RenderException(
                    $"the lettering has no glyph for '{character}' (U+{(int)character:X4}). " +
                    "Allowed: unaccented capital letters, period and space.");

            if (previous is { } before && PairKerning.TryGetValue((before, character), out var pull))
                cursor -= pull;
            previous = character;

            foreach (var stroke in glyph.Strokes)
                placed.Add(ShiftPath(stroke, cursor));

            textWidth = cursor + glyph.Width;
            cursor += glyph.Width + GlyphTable.Tracking;
        }

        return new LetteringPlacement(
            style.X,
            style.Baseline - style.CapHeight,
            scale,
            style.StrokeWidth ?? style.CapHeight * StrokeToCapRatio,
            textWidth * scale,
            [.. placed.Select((pathData, index) => new LetteringStroke(index, pathData))]);
    }

    internal static void EnsureValidCssClassList(string cssClass)
    {
        if (!CssClassListPattern().IsMatch(cssClass))
            throw new RenderException(
                $"class=\"{cssClass}\" is invalid (use plain CSS class names separated by spaces).");
    }

    private static string ShiftPath(string pathData, double offsetX)
    {
        var shifted = new StringBuilder();
        var command = '\0';
        var parameterIndex = 0;

        foreach (Match token in PathTokenPattern().Matches(pathData))
        {
            if (char.IsLetter(token.Value[0]))
            {
                command = token.Value[0];
                parameterIndex = 0;
                shifted.Append(command);
                continue;
            }

            var isHorizontal = command switch
            {
                'M' or 'L' => parameterIndex % 2 == 0,
                'A' => parameterIndex % 7 == 5,
                'H' => true,
                'V' => false,
                _ => throw new InvalidOperationException($"unsupported path command: '{command}'."),
            };

            if (parameterIndex > 0)
                shifted.Append(' ');
            shifted.Append(isHorizontal
                ? Num.Format(double.Parse(token.Value, CultureInfo.InvariantCulture) + offsetX)
                : token.Value);
            parameterIndex++;
        }

        return shifted.ToString();
    }

    [GeneratedRegex(@"[A-Za-z]|-?\d+(?:\.\d+)?")]
    private static partial Regex PathTokenPattern();

    [GeneratedRegex(@"\A[A-Za-z][A-Za-z0-9_-]*(?: [A-Za-z][A-Za-z0-9_-]*)*\z")]
    private static partial Regex CssClassListPattern();
}
