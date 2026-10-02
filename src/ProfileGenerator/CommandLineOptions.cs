using ProfileGenerator.Rendering;

namespace ProfileGenerator;

public sealed record CommandLineOptions(
    bool Offline = false,
    bool Force = false,
    bool ShowHelp = false,
    string? OutputDirectory = null,
    string? OnlyScene = null,
    Theme? OnlyTheme = null,
    string? TemplatesDirectory = null,
    string? RootDirectory = null)
{
    public const string UsageText = """
        Usage: dotnet run --project src/ProfileGenerator -- [options]

          (no options)       fetches the data, saves the snapshot and renders every scene in both themes
          --offline          uses data/contributions.json, no network
          --out DIR          output folder (default: <root>/assets)
          --scene NAME       renders only this scene (template .svg file name, without the extension)
          --theme dark|light renders only this theme
          --templates DIR    templates folder (default: <root>/src/ProfileGenerator/Templates)
          --root DIR         repository root (default: walks up from the current folder until it finds profile.json)
          --force            skips the guard that rejects a total below 40% of the previous one

        Environment variable GITHUB_TOKEN: uses the GitHub API (GraphQL). Without it, reads the public contributions page.
        On your machine, without a token, use --offline.
        Exit codes: 0 ok · 1 data failure · 2 usage or template error.
        """;

    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        var options = new CommandLineOptions();
        var position = 0;

        string ValueOf(string option) =>
            position + 1 < args.Count && !args[position + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++position]
                : throw new UsageException($"the option {option} needs a value.");

        while (position < args.Count)
        {
            var option = args[position];
            options = option switch
            {
                "--offline" => options with { Offline = true },
                "--force" => options with { Force = true },
                "--help" or "-h" => options with { ShowHelp = true },
                "--out" => options with { OutputDirectory = ValueOf(option) },
                "--scene" => options with { OnlyScene = ValueOf(option) },
                "--theme" => options with { OnlyTheme = ParseTheme(ValueOf(option)) },
                "--templates" => options with { TemplatesDirectory = ValueOf(option) },
                "--root" => options with { RootDirectory = ValueOf(option) },
                _ => throw new UsageException($"unknown option: {option}. Use --help to see the options."),
            };
            position++;
        }

        return options;
    }

    private static Theme ParseTheme(string value) => value switch
    {
        "dark" => Theme.Dark,
        "light" => Theme.Light,
        _ => throw new UsageException($"--theme accepts dark or light, but got \"{value}\"."),
    };
}
