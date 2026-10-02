namespace ProfileGenerator.Scenes.Terrain;

internal readonly record struct ScreenPoint(double X, double Y);

internal readonly record struct CameraPosition(double X, double Y, double Distance);

internal sealed class TerrainCamera
{
    public static readonly CameraPosition Final = new(1.35, 1.75, 2.6);

    public const double PeakHeight = 0.5;

    public const double BoxLeft = 52;
    public const double BoxTop = 238;
    public const double BoxRight = 794;
    public const double BoxBottom = 482;

    private readonly TerrainShape shape;
    private readonly double scale;
    private readonly double offsetX;
    private readonly double offsetY;

    public TerrainCamera(TerrainShape shape)
    {
        this.shape = shape;

        double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;
        void See(double x, double y, double z)
        {
            var (u, v) = Raw(x, y, z, Final);
            (left, right, top, bottom) = (Math.Min(left, u), Math.Max(right, u), Math.Min(top, v), Math.Max(bottom, v));
        }

        for (var row = 0; row < shape.Rows; row++)
        {
            for (var column = 0; column < shape.Columns; column++)
                See(X(column), shape.Height(row, column) * PeakHeight, Z(row));
        }

        foreach (var x in (double[])[-1, 1])
        {
            foreach (var z in (double[])[0, 1])
                See(x, 0, z);
        }

        scale = Math.Min((BoxRight - BoxLeft) / (right - left), (BoxBottom - BoxTop) / (bottom - top));
        offsetX = (BoxLeft + BoxRight) / 2 - scale * (left + right) / 2;
        offsetY = BoxBottom - scale * bottom;
    }

    public double X(int column) => -1 + 2.0 * column / (shape.Columns - 1);

    public double Z(double row) => 1 - row / (shape.Rows - 1);

    public ScreenPoint Project(double x, double y, double z)
    {
        var (u, v) = Raw(x, y, z, Final);
        return new ScreenPoint(offsetX + scale * u, offsetY + scale * v);
    }

    public ScreenPoint Surface(int row, int column) =>
        Project(X(column), shape.Height(row, column) * PeakHeight, Z(row));

    public double GroundY(int row) => Project(0, 0, Z(row)).Y;

    public (double Scale, double Dx, double Dy) PlaneSeenFrom(double z, CameraPosition from)
    {
        var planeScale = (z + Final.Distance) / (z + from.Distance);
        var dx = offsetX * (1 - planeScale) + scale * (Final.X - from.X) / (z + from.Distance);
        var dy = offsetY * (1 - planeScale) - scale * (Final.Y - from.Y) / (z + from.Distance);
        return (planeScale, dx, dy);
    }

    private static (double U, double V) Raw(double x, double y, double z, CameraPosition camera) =>
        ((x - camera.X) / (z + camera.Distance), -(y - camera.Y) / (z + camera.Distance));
}
