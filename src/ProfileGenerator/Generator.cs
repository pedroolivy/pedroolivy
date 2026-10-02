using System.Text;
using ProfileGenerator.Data;
using ProfileGenerator.Domain;
using ProfileGenerator.Rendering;
using ProfileGenerator.Scenes;

namespace ProfileGenerator;

public sealed class Generator(
    CommandLineOptions options,
    TimeProvider clock,
    HttpClient http,
    string? githubToken,
    TextWriter log)
{
    private const string ProfileFileName = "profile.json";

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var root = FindRoot(options.RootDirectory);
        var profile = Profile.Load(Path.Combine(root, ProfileFileName));

        var templatesDirectory = options.TemplatesDirectory ?? Path.Combine(root, "src", "ProfileGenerator", "Templates");
        var catalog = new SceneCatalog(templatesDirectory, SceneSlotProviders.CreateAll());
        var scenes = catalog.Select(options.OnlyScene);

        var snapshots = new SnapshotStore(Path.Combine(root, "data", "contributions.json"));
        var year = await LoadYearAsync(profile.Login, snapshots.Load(), cancellationToken);

        log.WriteLine(
            $"Data ({ContributionSourceNames.Format(year.Source)}): {ContributionYear.DayCount} days up to {year.To:O}, {year.Total} contributions.");

        var renderer = new SceneRenderer(new TemplateEngine(templatesDirectory, profile), catalog, profile);
        var files = renderer.Render(year, scenes, options.OnlyTheme);

        Write(files, Path.GetFullPath(options.OutputDirectory ?? Path.Combine(root, "assets")));
        if (!options.Offline)
            snapshots.Save(year);
    }

    private async Task<ContributionYear> LoadYearAsync(
        string login, ContributionYear? snapshot, CancellationToken cancellationToken)
    {
        if (snapshot is not null && !string.Equals(snapshot.Login, login, StringComparison.OrdinalIgnoreCase))
        {
            if (options.Offline)
                throw new DataException(
                    $"data/contributions.json belongs to the login \"{snapshot.Login}\", but {ProfileFileName} says \"{login}\". " +
                    "Run without --offline to fetch the right data.");

            log.WriteLine(
                $"Warning: data/contributions.json belongs to the login \"{snapshot.Login}\"; the 40% guard will not compare against it.");
            snapshot = null;
        }

        if (!options.Offline)
            return await FetchAsync(login, snapshot, cancellationToken);

        return snapshot ?? throw new DataException("there is no data/contributions.json; run without --offline to fetch the data.");
    }

    private async Task<ContributionYear> FetchAsync(
        string login, ContributionYear? previous, CancellationToken cancellationToken)
    {
        var window = ContributionWindow.EndingBefore(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));

        var sources = new List<IContributionSource>();
        if (!string.IsNullOrEmpty(githubToken))
            sources.Add(new GitHubGraphQlSource(http, githubToken));
        sources.Add(new PublicCalendarSource(http));

        var fetcher = new ContributionFetcher(sources, message => log.WriteLine($"Warning: {message}"));
        var year = await fetcher.FetchAsync(login, window, cancellationToken);

        if (!options.Force)
            year.EnsureNotCollapsedFrom(previous);

        return year;
    }

    private void Write(IReadOnlyList<RenderedFile> files, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (var file in files)
        {
            File.WriteAllText(Path.Combine(outputDirectory, file.FileName), file.Content, Utf8WithoutBom);
            log.WriteLine($"Rendered {file.FileName} ({Num.Format(Utf8WithoutBom.GetByteCount(file.Content) / 1024.0)} KB)");
        }

        log.WriteLine($"Done: {files.Count} file(s) in {outputDirectory}");
    }

    private static string FindRoot(string? explicitRoot)
    {
        if (explicitRoot is not null)
        {
            var root = Path.GetFullPath(explicitRoot);
            return File.Exists(Path.Combine(root, ProfileFileName))
                ? root
                : throw new UsageException($"could not find {ProfileFileName} in {root}.");
        }

        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProfileFileName)))
                return directory.FullName;
        }

        throw new UsageException($"could not find {ProfileFileName} walking up from the current folder. Use --root DIR.");
    }
}
