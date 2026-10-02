using System.Globalization;
using System.Text.Json;

namespace ProfileGenerator.Rendering;

public sealed class Profile
{
    private readonly JsonElement root;

    private Profile(JsonElement root)
    {
        this.root = root;
        Login = Text("login");
    }

    public string Login { get; }

    public static Profile Load(string filePath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(filePath));
            return new Profile(document.RootElement.Clone());
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new RenderException($"could not read {filePath} (technical detail: {ex.Message})");
        }
    }

    public string Text(string path) =>
        Navigate(path) is { ValueKind: JsonValueKind.String } node
            ? node.GetString()!
            : throw new RenderException($"profile.json: \"{path}\" is not a string.");

    private JsonElement Navigate(string path)
    {
        var node = root;
        foreach (var segment in path.Split('.'))
        {
            node = node.ValueKind switch
            {
                JsonValueKind.Object when node.TryGetProperty(segment, out var property) => property,
                JsonValueKind.Array when int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var position)
                                         && position < node.GetArrayLength() => node[position],
                _ => throw new RenderException($"profile.json: the path \"{path}\" does not exist (stopped at \"{segment}\")."),
            };
        }

        return node;
    }
}
