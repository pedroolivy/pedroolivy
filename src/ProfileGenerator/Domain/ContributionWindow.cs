namespace ProfileGenerator.Domain;

public readonly record struct ContributionWindow(DateOnly From, DateOnly To)
{
    public static ContributionWindow EndingBefore(DateOnly todayUtc)
    {
        var yesterday = todayUtc.AddDays(-1);
        return new ContributionWindow(yesterday.AddDays(1 - ContributionYear.DayCount), yesterday);
    }
}
