namespace ProfileGenerator.Rendering;

public static class Fonts
{
    public const string Mono = "ui-monospace, 'Cascadia Mono', 'SF Mono', Menlo, Consolas, 'DejaVu Sans Mono', monospace";

    public static string? Resolve(string name) => name == "mono" ? Mono : null;
}
