namespace ProfileGenerator.Domain;

public sealed class ContributionYear
{
    public const int WeekCount = 52;
    public const int DayCount = WeekCount * 7;

    private const int CollapseThresholdPercent = 40;

    private ContributionYear(
        string login,
        ContributionSource source,
        IReadOnlyList<ContributionDay> days,
        int? reportedTotal)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new DataException("the login is empty.");
        if (days.Count != DayCount)
            throw new DataException($"expected {DayCount} days, but got {days.Count}.");

        for (var index = 0; index < days.Count; index++)
        {
            var expectedDate = days[0].Date.AddDays(index);
            if (days[index].Date != expectedDate)
                throw new DataException($"days out of sequence: expected {expectedDate:O}, got {days[index].Date:O}.");
            if (days[index].Count < 0)
                throw new DataException($"negative count on {days[index].Date:O}.");
        }

        var longTotal = days.Sum(day => (long)day.Count);
        if (longTotal > int.MaxValue)
            throw new DataException($"the sum of the days ({longTotal}) is above {int.MaxValue}.");

        var total = (int)longTotal;
        if (reportedTotal is { } reported && reported != total)
            throw new DataException($"the reported total ({reported}) differs from the sum of the days ({total}).");

        Login = login;
        Source = source;
        Days = [.. days];
        Total = total;
    }

    public static ContributionYear FromDays(
        string login,
        ContributionSource source,
        IReadOnlyList<ContributionDay> days,
        int? reportedTotal) =>
        new(login, source, days, reportedTotal);

    public string Login { get; }
    public ContributionSource Source { get; }

    public IReadOnlyList<ContributionDay> Days { get; }

    public int Total { get; }

    public DateOnly From => Days[0].Date;
    public DateOnly To => Days[^1].Date;

    public void EnsureNotCollapsedFrom(ContributionYear? previous)
    {
        if (previous is null)
            return;

        if (Total * 100L < previous.Total * (long)CollapseThresholdPercent)
            throw new DataException(
                $"the new total ({Total}) is less than {CollapseThresholdPercent}% of the previous one ({previous.Total}). " +
                "The profile may be hiding contributions. Use --force if this is intended.");
    }
}
