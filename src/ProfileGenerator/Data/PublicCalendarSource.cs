using System.Globalization;
using System.Text.RegularExpressions;
using ProfileGenerator.Domain;

namespace ProfileGenerator.Data;

public sealed partial class PublicCalendarSource(HttpClient http) : IContributionSource
{
    public string Name => "GitHub public calendar";

    public async Task<ContributionYear> FetchAsync(
        string login, ContributionWindow window, CancellationToken cancellationToken)
    {
        var address = $"https://github.com/users/{Uri.EscapeDataString(login)}/contributions";
        var html = await HttpRetry.SendForTextAsync(
            http, () => new HttpRequestMessage(HttpMethod.Get, address), cancellationToken);

        return Parse(login, html, window);
    }

    private static ContributionYear Parse(string login, string html, ContributionWindow window)
    {
        var tooltipTextByCellId = new Dictionary<string, string>();
        foreach (Match tooltip in TooltipPattern().Matches(html))
            tooltipTextByCellId.TryAdd(tooltip.Groups["cell"].Value, tooltip.Groups["text"].Value);

        var days = new List<ContributionDay>();
        foreach (Match cell in CellPattern().Matches(html))
        {
            var rawDate = cell.Groups["date"].Value;
            if (!DateOnly.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                throw new DataException($"unexpected date in a cell: \"{rawDate}\".");

            if (date < window.From || date > window.To)
                continue;

            if (!tooltipTextByCellId.TryGetValue(cell.Groups["id"].Value, out var tooltipText))
                throw new DataException($"the cell for {date:O} has no tooltip with the count.");
            days.Add(new ContributionDay(date, ReadCount(tooltipText)));
        }

        days.Sort((left, right) => left.Date.CompareTo(right.Date));
        return ContributionYear.FromDays(login, ContributionSource.Public, days, reportedTotal: null);
    }

    private static int ReadCount(string tooltipText)
    {
        var match = CountPattern().Match(tooltipText);
        if (!match.Success)
            throw new DataException($"unexpected tooltip text: \"{tooltipText.Trim()}\".");

        var number = match.Groups["number"].Value;
        return number.Length == 0 ? 0 : int.Parse(NonDigitPattern().Replace(number, ""), CultureInfo.InvariantCulture);
    }

    [GeneratedRegex("""<td\b(?=[^>]*\sdata-date="(?<date>[^"]+)")(?=[^>]*\sid="(?<id>[^"]+)")[^>]*>""")]
    private static partial Regex CellPattern();

    [GeneratedRegex("""<tool-tip\b[^>]*\sfor="(?<cell>[^"]+)"[^>]*>(?<text>[^<]*)</tool-tip>""")]
    private static partial Regex TooltipPattern();

    [GeneratedRegex(@"^\s*(?:No|(?<number>[\d.,]+))\s+contributions?\b")]
    private static partial Regex CountPattern();

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitPattern();
}
