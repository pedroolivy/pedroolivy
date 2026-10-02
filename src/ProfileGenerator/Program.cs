using System.Text;
using ProfileGenerator;
using ProfileGenerator.Domain;
using ProfileGenerator.Rendering;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    var options = CommandLineOptions.Parse(args);
    if (options.ShowHelp)
    {
        Console.WriteLine(CommandLineOptions.UsageText);
        return 0;
    }

    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("profile-generator (+https://github.com/pedroolivy/pedroolivy)");

    var generator = new Generator(
        options, TimeProvider.System, http, Environment.GetEnvironmentVariable("GITHUB_TOKEN"), Console.Error);
    await generator.RunAsync(cancellation.Token);
    return 0;
}
catch (DataException ex)
{
    Console.Error.WriteLine($"Data error: {ex.Message}");
    return 1;
}
catch (Exception ex) when (ex is UsageException or RenderException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 2;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error reading or writing a file (technical detail: {ex.Message})");
    return 1;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Cancelled. Nothing was written.");
    return 130;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error, this is a generator bug: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
