using System.Security;
using System.Text;

namespace ProfileGenerator.Rendering;

public sealed record RenderContext(
    Palette Palette,
    IReadOnlyDictionary<string, string> Data,
    IReadOnlyDictionary<string, string> Slots);

public sealed class TemplateEngine(string templatesDirectory, Profile profile)
{
    public string Render(string sceneName, RenderContext context)
    {
        var fileName = sceneName + ".svg";
        var source = ReadTemplate(fileName);
        var output = new StringBuilder(source.Length);
        var cursor = 0;

        while (true)
        {
            var open = source.IndexOf("{{", cursor, StringComparison.Ordinal);
            if (open < 0)
            {
                output.Append(source, cursor, source.Length - cursor);
                return output.ToString();
            }

            output.Append(source, cursor, open - cursor);

            var close = source.IndexOf("}}", open + 2, StringComparison.Ordinal);
            if (close < 0)
                throw new RenderException(
                    $"{fileName}: unclosed token near \"{source[open..Math.Min(source.Length, open + 30)]}\".");

            var token = source[(open + 2)..close];
            cursor = close + 2;

            var colon = token.IndexOf(':');
            var (kind, argument) = colon < 0 ? (token, "") : (token[..colon], token[(colon + 1)..]);

            try
            {
                output.Append(Resolve(kind, argument, context));
            }
            catch (RenderException ex)
            {
                throw new RenderException($"{fileName}: {{{{{token}}}}}: {ex.Message}");
            }
        }
    }

    private string Resolve(string kind, string argument, RenderContext context) => kind switch
    {
        "color" => context.Palette.Resolve(argument)
                   ?? throw new RenderException($"unknown color \"{argument}\"."),
        "font" => Fonts.Resolve(argument)
                  ?? throw new RenderException($"unknown font \"{argument}\" (use mono)."),
        "text" => Escape(profile.Text(argument)),
        "data" => context.Data.TryGetValue(argument, out var value)
            ? Escape(value)
            : throw new RenderException($"unknown data key \"{argument}\"."),
        "slot" => context.Slots.TryGetValue(argument, out var fragment)
            ? fragment
            : throw new RenderException($"this scene has no slot \"{argument}\"."),
        _ => throw new RenderException($"unknown token \"{kind}\"."),
    };

    private string ReadTemplate(string fileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(templatesDirectory, fileName));
        if (!File.Exists(fullPath))
            throw new RenderException($"template not found: {fullPath}");

        try
        {
            return File.ReadAllText(fullPath).Replace("\r\n", "\n");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new RenderException($"could not read the template {fullPath} (technical detail: {ex.Message})");
        }
    }

    private static string Escape(string text) => SecurityElement.Escape(text)!;
}
