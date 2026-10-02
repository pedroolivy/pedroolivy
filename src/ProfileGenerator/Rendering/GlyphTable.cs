using System.Collections.Frozen;

namespace ProfileGenerator.Rendering;

internal readonly record struct Glyph(double Width, string[] Strokes);

internal static class GlyphTable
{
    public const double CapHeight = 100;
    public const double Tracking = 26;
    public const double WordSpace = 70;

    public static readonly FrozenDictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>
    {
        ['A'] = new(88, ["M0 100 L44 0 L88 100", "M24 66 H64"]),
        ['B'] = new(58, ["M0 0 V100", "M9 0 H30 A24 24 0 0 1 30 48 H9", "M9 100 H32 A26 26 0 0 0 32 48"]),
        ['C'] = new(88, ["M88.3 17.9 A50 50 0 1 0 88.3 82.1"]),
        ['D'] = new(68, ["M0 0 V100", "M9 0 H18 A50 50 0 0 1 18 100 H9"]),
        ['E'] = new(54, ["M0 0 V100", "M9 0 H54", "M9 50 H44", "M9 100 H54"]),
        ['F'] = new(52, ["M0 0 V100", "M9 0 H52", "M9 50 H42"]),
        ['G'] = new(100, ["M88.3 17.9 A50 50 0 1 0 100 50 H56"]),
        ['H'] = new(70, ["M0 0 V100", "M70 0 V100", "M9 50 H61"]),
        ['I'] = new(0, ["M0 0 V100"]),
        ['J'] = new(48, ["M48 0 V76 A24 24 0 0 1 0 76"]),
        ['K'] = new(64, ["M0 0 V100", "M62 0 L9 47.9", "M26.9 43.6 L64 100"]),
        ['L'] = new(50, ["M0 0 V100 H50"]),
        ['M'] = new(100, ["M0 100 V0 L50 80 L100 0 V100"]),
        ['N'] = new(76, ["M0 100 V0 L76 100 V0"]),
        ['O'] = new(100, ["M45.5 0.2 A50 50 0 0 0 45.5 99.8", "M54.5 0.2 A50 50 0 0 1 54.5 99.8"]),
        ['P'] = new(60, ["M0 0 V100", "M9 0 H32 A28 28 0 0 1 32 56 H9"]),
        ['Q'] = new(100, ["M45.5 0.2 A50 50 0 0 0 45.5 99.8", "M54.5 0.2 A50 50 0 0 1 54.5 99.8", "M62 66 L98 104"]),
        ['R'] = new(62, ["M0 0 V100", "M9 0 H32 A28 28 0 0 1 32 56 H9", "M35.3 63.3 L62 100"]),
        ['S'] = new(53, ["M47.8 12 A24 24 0 1 0 27 48 A26 26 0 1 1 4.5 87"]),
        ['T'] = new(72, ["M0 0 H72", "M36 9 V100"]),
        ['U'] = new(72, ["M0 0 V64 A36 36 0 0 0 72 64 V0"]),
        ['V'] = new(84, ["M0 0 L42 100 L84 0"]),
        ['W'] = new(124, ["M0 0 L31 100 L62 0 L93 100 L124 0"]),
        ['X'] = new(76, ["M0 0 L76 100", "M76 0 L0 100"]),
        ['Y'] = new(80, ["M0 0 L40 52 L80 0", "M40 61 V100"]),
        ['Z'] = new(70, ["M0 0 H70 L0 100 H70"]),
        ['.'] = new(0, ["M0 92 V100"]),
    }.ToFrozenDictionary();
}
