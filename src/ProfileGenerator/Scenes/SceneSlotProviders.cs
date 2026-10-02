namespace ProfileGenerator.Scenes;

public static class SceneSlotProviders
{
    public static IReadOnlyList<ISceneSlotProvider> CreateAll() => [new HeroScene()];
}
