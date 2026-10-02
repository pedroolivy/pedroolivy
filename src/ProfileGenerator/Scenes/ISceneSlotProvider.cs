using ProfileGenerator.Domain;
using ProfileGenerator.Rendering;

namespace ProfileGenerator.Scenes;

public sealed record SceneContext(
    IReadOnlyList<ContributionDay> Days,
    Palette Palette,
    Profile Profile);

public interface ISceneSlotProvider
{
    string SceneName { get; }

    IReadOnlyDictionary<string, string> BuildSlots(SceneContext context);
}
