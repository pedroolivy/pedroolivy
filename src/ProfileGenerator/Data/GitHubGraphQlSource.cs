using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ProfileGenerator.Domain;

namespace ProfileGenerator.Data;

public sealed class GitHubGraphQlSource(HttpClient http, string token) : IContributionSource
{
    private const string Endpoint = "https://api.github.com/graphql";

    private const string Query = """
        query($login: String!, $from: DateTime!, $to: DateTime!) {
          user(login: $login) {
            contributionsCollection(from: $from, to: $to) {
              contributionCalendar {
                totalContributions
                weeks { contributionDays { date contributionCount } }
              }
            }
          }
        }
        """;

    public string Name => "GitHub GraphQL";

    public async Task<ContributionYear> FetchAsync(
        string login, ContributionWindow window, CancellationToken cancellationToken)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            query = Query,
            variables = new { login, from = $"{window.From:O}T00:00:00Z", to = $"{window.To:O}T23:59:59Z" },
        });

        var responseBody = await HttpRetry.SendForTextAsync(
            http,
            () => new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            },
            cancellationToken);

        return Parse(login, responseBody);
    }

    private static ContributionYear Parse(string login, string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            {
                var messages = errors.EnumerateArray().Select(error => error.GetProperty("message").GetString());
                throw new DataException($"GraphQL returned an error: {string.Join("; ", messages)}");
            }

            var user = root.GetProperty("data").GetProperty("user");
            if (user.ValueKind == JsonValueKind.Null)
                throw new DataException($"the user \"{login}\" does not exist on GitHub.");

            var collection = user.GetProperty("contributionsCollection");
            var calendar = collection.GetProperty("contributionCalendar");
            var days = calendar.GetProperty("weeks").EnumerateArray()
                .SelectMany(week => week.GetProperty("contributionDays").EnumerateArray())
                .Select(day => new ContributionDay(
                    DateOnly.ParseExact(day.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    day.GetProperty("contributionCount").GetInt32()))
                .ToList();

            return ContributionYear.FromDays(
                login,
                ContributionSource.GraphQl,
                days,
                reportedTotal: calendar.GetProperty("totalContributions").GetInt32());
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException
                                       or FormatException or ArgumentException)
        {
            throw new DataException($"unexpected GraphQL response format (technical detail: {ex.Message}).", ex);
        }
    }
}
