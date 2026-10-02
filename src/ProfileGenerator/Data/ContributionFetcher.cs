using ProfileGenerator.Domain;

namespace ProfileGenerator.Data;

public sealed class ContributionFetcher(IReadOnlyList<IContributionSource> sources, Action<string> warn)
{
    public async Task<ContributionYear> FetchAsync(
        string login, ContributionWindow window, CancellationToken cancellationToken)
    {
        foreach (var source in sources)
        {
            try
            {
                var year = await source.FetchAsync(login, window, cancellationToken);
                if (year.From != window.From || year.To != window.To)
                    throw new DataException(
                        $"the window came from {year.From:O} to {year.To:O}, but {window.From:O} to {window.To:O} was expected.");

                return year;
            }
            catch (DataException ex)
            {
                warn($"Source \"{source.Name}\" failed: {ex.Message}");
            }
        }

        throw new DataException("no data source worked; the current snapshot was kept.");
    }
}
