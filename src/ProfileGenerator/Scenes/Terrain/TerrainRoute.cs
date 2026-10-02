namespace ProfileGenerator.Scenes.Terrain;

internal sealed class TerrainRoute
{
    private const int CrestSmoothingRadius = 20;

    private readonly double[] cumulative;

    public TerrainRoute(TerrainShape shape, TerrainCamera camera)
    {
        var crestRows = Enumerable.Range(0, shape.Columns).Select(column =>
        {
            var best = 0;
            for (var row = 1; row < shape.Rows; row++)
            {
                if (shape.Height(row, column) > shape.Height(best, column))
                    best = row;
            }

            return best;
        }).ToArray();

        Depths = [.. Enumerable.Range(0, shape.Columns).Select(column =>
        {
            double sum = 0, weightSum = 0;
            for (var offset = -CrestSmoothingRadius; offset <= CrestSmoothingRadius; offset++)
            {
                var weight = CrestSmoothingRadius + 1 - Math.Abs(offset);
                sum += weight * crestRows[Math.Clamp(column + offset, 0, shape.Columns - 1)];
                weightSum += weight;
            }

            return sum / weightSum;
        })];

        Points = [.. Depths.Select((depth, column) => camera.Project(camera.X(column), HeightAt(shape, depth, column) * TerrainCamera.PeakHeight, camera.Z(depth)))];

        cumulative = new double[Points.Count];
        for (var index = 1; index < Points.Count; index++)
            cumulative[index] = cumulative[index - 1] + Distance(Points[index - 1], Points[index]);
    }

    public IReadOnlyList<double> Depths { get; }

    public IReadOnlyList<ScreenPoint> Points { get; }

    public double Length => cumulative[^1];

    public ScreenPoint End => Points[^1];

    public double LengthAt(int column) => cumulative[column];

    public (ScreenPoint Point, double Depth) At(double length)
    {
        var index = 1;
        while (index < Points.Count - 1 && cumulative[index] < length)
            index++;

        var span = cumulative[index] - cumulative[index - 1];
        var t = span > 0 ? (length - cumulative[index - 1]) / span : 0;
        var (from, to) = (Points[index - 1], Points[index]);
        return (new ScreenPoint(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t),
                Depths[index - 1] + (Depths[index] - Depths[index - 1]) * t);
    }

    private static double HeightAt(TerrainShape shape, double depth, int column)
    {
        var back = (int)Math.Floor(depth);
        var front = Math.Min(shape.Rows - 1, back + 1);
        var t = depth - back;
        return shape.Height(back, column) * (1 - t) + shape.Height(front, column) * t;
    }

    private static double Distance(ScreenPoint a, ScreenPoint b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
}
