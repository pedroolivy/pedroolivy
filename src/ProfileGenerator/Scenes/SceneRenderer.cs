using System.Xml;
using System.Xml.Linq;
using ProfileGenerator.Domain;
using ProfileGenerator.Rendering;

namespace ProfileGenerator.Scenes;

public sealed record RenderedFile(string FileName, string Content);

public sealed class SceneRenderer(TemplateEngine engine, SceneCatalog catalog, Profile profile)
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";
    private static readonly IReadOnlyDictionary<string, string> NoSlots = new Dictionary<string, string>();

    public IReadOnlyList<RenderedFile> Render(
        ContributionYear year, IReadOnlyList<string> scenes, Theme? onlyTheme)
    {
        Theme[] themes = onlyTheme is { } theme ? [theme] : [Theme.Dark, Theme.Light];
        var data = TemplateData.From(year);
        var files = new List<RenderedFile>();

        foreach (var scene in scenes)
        {
            foreach (var palette in themes.Select(Palette.For))
            {
                var slots = catalog.ProviderFor(scene)?.BuildSlots(new SceneContext(year.Days, palette, profile))
                            ?? NoSlots;
                var fileName = $"{scene}-{palette.Name}.svg";
                var svg = engine.Render(scene, new RenderContext(palette, data, slots));

                Validate(fileName, svg);
                files.Add(new RenderedFile(fileName, svg));
            }
        }

        return files;
    }

    private static void Validate(string fileName, string svg)
    {
        if (svg.Contains("{{", StringComparison.Ordinal))
            throw new RenderException($"{fileName}: \"{{{{\" left after rendering (misspelled token or incomplete slot).");

        try
        {
            if (XDocument.Parse(svg).Root?.Name != SvgNamespace + "svg")
                throw new RenderException($"{fileName}: the root must be <svg xmlns=\"http://www.w3.org/2000/svg\">.");
        }
        catch (XmlException ex)
        {
            throw new RenderException($"{fileName}: the rendered SVG is not valid XML (technical detail: {ex.Message})");
        }
    }
}
