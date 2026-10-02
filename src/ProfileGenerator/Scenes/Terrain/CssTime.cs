namespace ProfileGenerator.Scenes.Terrain;

internal static class CssTime
{
    public static string Ms(double seconds) =>
        $"{(int)Math.Round(seconds * 1000, MidpointRounding.AwayFromZero)}ms";
}
