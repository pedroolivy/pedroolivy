using ProfileGenerator.Domain;

namespace ProfileGenerator.Scenes.Terrain;

internal sealed class TerrainShape
{
    public const int SamplesPerWeek = 4;
    public const int SamplesPerDay = 4;

    private const int DaysPerWeek = 7;

    private const int FullHeightDailyCount = 8;

    private readonly double[][] heights;

    public TerrainShape(IReadOnlyList<ContributionDay> days)
    {
        if (days.Count != ContributionYear.DayCount)
            throw new ArgumentException($"the terrain needs {ContributionYear.DayCount} days.", nameof(days));

        WeekStarts = [.. Enumerable.Range(0, ContributionYear.WeekCount).Select(week => days[week * DaysPerWeek].Date)];

        var smooth = Blur(DailyGrid(days, out var busiestDay));
        Normalize(smooth, busiestDay);

        var alongTime = Enumerable.Range(0, DaysPerWeek)
            .Select(day => Resample([.. smooth.Select(week => week[day])], SamplesPerWeek))
            .ToArray();

        Columns = alongTime[0].Length;
        Rows = (DaysPerWeek - 1) * SamplesPerDay + 1;
        heights = [.. Enumerable.Range(0, Rows).Select(_ => new double[Columns])];
        for (var column = 0; column < Columns; column++)
        {
            var acrossDepth = Resample([.. alongTime.Select(line => line[column])], SamplesPerDay);
            for (var row = 0; row < Rows; row++)
                heights[row][column] = acrossDepth[row];
        }
    }

    public int Columns { get; }

    public int Rows { get; }

    public IReadOnlyList<DateOnly> WeekStarts { get; }

    public double Height(int row, int column) => heights[row][column];

    public static bool IsDayRow(int row) => row % SamplesPerDay == 0;

    private static double[][] DailyGrid(IReadOnlyList<ContributionDay> days, out int busiestDay)
    {
        busiestDay = days.Max(day => day.Count);
        var peak = Math.Max(busiestDay, 1);
        return
        [
            .. Enumerable.Range(0, ContributionYear.WeekCount).Select(week =>
                Enumerable.Range(0, DaysPerWeek).Select(day => Math.Sqrt((double)days[week * DaysPerWeek + day].Count / peak)).ToArray()),
        ];
    }

    private static double[][] Blur(double[][] grid)
    {
        var weeks = grid.Length;
        double At(int week, int day) => grid[Math.Clamp(week, 0, weeks - 1)][Math.Clamp(day, 0, DaysPerWeek - 1)];

        return
        [
            .. Enumerable.Range(0, weeks).Select(week => Enumerable.Range(0, DaysPerWeek).Select(day =>
            {
                double sum = 0, weightSum = 0;
                for (var dw = -1; dw <= 1; dw++)
                {
                    for (var dd = -1; dd <= 1; dd++)
                    {
                        var weight = (2 - Math.Abs(dw)) * (2 - Math.Abs(dd));
                        sum += weight * At(week + dw, day + dd);
                        weightSum += weight;
                    }
                }

                return sum / weightSum;
            }).ToArray()),
        ];
    }

    private static void Normalize(double[][] grid, int busiestDay)
    {
        var highest = grid.Max(week => week.Max());
        if (highest <= 0)
            return;

        var amplitude = Math.Sqrt(Math.Min(1.0, (double)busiestDay / FullHeightDailyCount));
        foreach (var week in grid)
        {
            for (var day = 0; day < week.Length; day++)
                week[day] = week[day] / highest * amplitude;
        }
    }

    private static double[] Resample(double[] values, int factor)
    {
        double At(int index) => values[Math.Clamp(index, 0, values.Length - 1)];

        var result = new List<double>((values.Length - 1) * factor + 1);
        for (var index = 0; index < values.Length - 1; index++)
        {
            for (var step = 0; step < factor; step++)
            {
                var t = (double)step / factor;
                var (p0, p1, p2, p3) = (At(index - 1), At(index), At(index + 1), At(index + 2));
                var value = 0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);
                result.Add(Math.Max(0, value));
            }
        }

        result.Add(values[^1]);
        return [.. result];
    }
}
