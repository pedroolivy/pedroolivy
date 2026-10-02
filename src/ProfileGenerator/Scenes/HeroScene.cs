using System.Security;
using System.Text;
using ProfileGenerator.Rendering;
using ProfileGenerator.Scenes.Terrain;

namespace ProfileGenerator.Scenes;

public sealed class HeroScene : ISceneSlotProvider
{
    private const double NameLeft = 56;
    private const double NameRight = 790;
    private const double NameBaseline = 162;
    private const double NameStrokeWidth = 4;

    public string SceneName => "profile";

    public IReadOnlyDictionary<string, string> BuildSlots(SceneContext context)
    {
        var shape = new TerrainShape(context.Days);
        var camera = new TerrainCamera(shape);
        var route = new TerrainRoute(shape, camera);
        var nowLabel = SecurityElement.Escape(context.Profile.Text("hero.now"));
        var terrain = TerrainMarkup.Build(shape, camera, route, context.Palette, nowLabel);
        var name = PlaceName(context.Profile.Text("hero.name"));

        return new Dictionary<string, string>
        {
            ["name"] = name.ToSvg("name"),
            ["terrain"] = terrain.Markup,
            ["terrain-defs"] = terrain.Defs,
            ["terrain-css"] = NameCss(name) + terrain.Css,
        };
    }

    private static string NameCss(LetteringPlacement name)
    {
        var css = new StringBuilder(
            $".name .ls{{animation:draw {CssTime.Ms(TerrainTimeline.NameStrokeSeconds)} cubic-bezier(.6,0,.2,1) both}}\n");
        foreach (var stroke in name.Strokes)
        {
            var delay = TerrainTimeline.NameAt + stroke.Index * TerrainTimeline.NameStrokeStep;
            css.Append($".name .ls:nth-child({stroke.Index + 1}){{animation-delay:{CssTime.Ms(delay)}}}\n");
        }

        return css.ToString();
    }

    private static LetteringPlacement PlaceName(string text)
    {
        var widthAtHundred = Lettering.Place(text, new LetteringStyle(X: 0, Baseline: 100, CapHeight: 100)).Width;
        var axisWidth = NameRight - NameLeft - NameStrokeWidth;
        var capHeight = axisWidth / widthAtHundred * 100;

        return Lettering.Place(
            text,
            new LetteringStyle(
                X: NameLeft + NameStrokeWidth / 2,
                Baseline: NameBaseline,
                CapHeight: capHeight,
                StrokeWidth: NameStrokeWidth));
    }
}
