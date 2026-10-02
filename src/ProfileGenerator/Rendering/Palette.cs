namespace ProfileGenerator.Rendering;

public enum Theme
{
    Dark,
    Light,
}

public sealed record Palette(
    string Name,
    string Bg,
    string Line,
    string Hatch,
    string Ink,
    string Soft,
    string Muted,
    string Signal,
    string Far,
    string Near,
    string Fog,
    string Floor,
    string Spark)
{
    public static Palette Dark { get; } = new(
        "dark", "#0C0B09", "#2A2722", "#4A453C", "#ECE6D8", "#B8B1A2", "#8F897C", "#FF4F2E",
        "#3A362F", "#D9D2C2", "#211E19", "#57514A", "#FFD9C9");

    public static Palette Light { get; } = new(
        "light", "#F1ECDF", "#D6CDB8", "#BDB29A", "#16140F", "#4A453B", "#6B6557", "#C5321A",
        "#CDC3AD", "#23201B", "#FBF8F1", "#A0977F", "#FFF6EE");

    public static Palette For(Theme theme) => theme switch
    {
        Theme.Dark => Dark,
        Theme.Light => Light,
        _ => throw new ArgumentOutOfRangeException(nameof(theme)),
    };

    public string? Resolve(string colorName) => colorName switch
    {
        "bg" => Bg,
        "line" => Line,
        "hatch" => Hatch,
        "ink" => Ink,
        "soft" => Soft,
        "muted" => Muted,
        "signal" => Signal,
        "far" => Far,
        "near" => Near,
        "fog" => Fog,
        "floor" => Floor,
        "spark" => Spark,
        _ => null,
    };
}
