namespace ProfileGenerator.Scenes.Terrain;

internal sealed class CubicBezier(double x1, double y1, double x2, double y2)
{
    private const int Iterations = 40;

    public double Progress(double time)
    {
        double low = 0, high = 1, t = time;
        for (var step = 0; step < Iterations; step++)
        {
            t = (low + high) / 2;
            if (Coordinate(t, x1, x2) < time)
                low = t;
            else
                high = t;
        }

        return Coordinate(t, y1, y2);
    }

    public double TimeAt(double progress)
    {
        double low = 0, high = 1;
        for (var step = 0; step < Iterations; step++)
        {
            var middle = (low + high) / 2;
            if (Progress(middle) < progress)
                low = middle;
            else
                high = middle;
        }

        return (low + high) / 2;
    }

    private static double Coordinate(double t, double first, double second) =>
        3 * (1 - t) * (1 - t) * t * first + 3 * (1 - t) * t * t * second + t * t * t;
}
