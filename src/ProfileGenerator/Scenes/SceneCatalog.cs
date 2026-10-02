using ProfileGenerator.Rendering;

namespace ProfileGenerator.Scenes;

public sealed class SceneCatalog
{
    private readonly Dictionary<string, ISceneSlotProvider> providersBySceneName;

    public SceneCatalog(string templatesDirectory, IEnumerable<ISceneSlotProvider> providers)
    {
        if (!Directory.Exists(templatesDirectory))
            throw new RenderException($"the templates folder does not exist: {templatesDirectory}");

        SceneNames = ListSceneNames(templatesDirectory);
        if (SceneNames.Count == 0)
            throw new RenderException($"there is no .svg template in {templatesDirectory}");

        providersBySceneName = providers.ToDictionary(provider => provider.SceneName, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> SceneNames { get; }

    private static IReadOnlyList<string> ListSceneNames(string templatesDirectory)
    {
        try
        {
            return
            [
                .. Directory.EnumerateFiles(templatesDirectory)
                    .Where(file => file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    .Select(file => Path.GetFileNameWithoutExtension(file))
                    .Order(StringComparer.Ordinal),
            ];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new RenderException(
                $"could not list the templates folder {templatesDirectory} (technical detail: {ex.Message})");
        }
    }

    public ISceneSlotProvider? ProviderFor(string sceneName) => providersBySceneName.GetValueOrDefault(sceneName);

    public IReadOnlyList<string> Select(string? sceneName) =>
        sceneName is null ? SceneNames
        : SceneNames.Contains(sceneName) ? [sceneName]
        : throw new UsageException($"the scene \"{sceneName}\" does not exist. Available scenes: {string.Join(", ", SceneNames)}.");
}
