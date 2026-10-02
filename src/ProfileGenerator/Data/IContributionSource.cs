using ProfileGenerator.Domain;

namespace ProfileGenerator.Data;

public interface IContributionSource
{
    string Name { get; }

    Task<ContributionYear> FetchAsync(string login, ContributionWindow window, CancellationToken cancellationToken);
}
