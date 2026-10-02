using System.Globalization;
using System.Text;
using ProfileGenerator.Domain;
using ProfileGenerator.Rendering;
using static ProfileGenerator.Scenes.Terrain.TerrainTimeline;

namespace ProfileGenerator.Scenes.Terrain;

internal sealed record TerrainSlots(string Defs, string Markup, string Css);

internal static class TerrainMarkup
{
    private static readonly CubicBezier RouteEase = new(0.45, 0, 0.3, 1);
    private static readonly CubicBezier FlightEase = new(0.32, 0, 0.18, 1);

    private static readonly CameraPosition FlightStart = new(0.0, 0.78, 0.88);
    private const int FlightFrames = 16;

    private const double FlowDash = 1.6;
    private const double FlowSpacing = 17;

    private const int CarrierSteps = 90;
    private const double PoleHeight = 34;

    private const double TableFront = -0.5;
    private const double TableBack = 2.2;
    private const double TableLeft = -3.4;
    private const double TableRight = 3.6;

    private const string MonthLetters = "JFMAMJJASOND";

    public static TerrainSlots Build(TerrainShape shape, TerrainCamera camera, TerrainRoute route, Palette palette, string nowLabel)
    {
        var css = new StringBuilder();
        var markup = new StringBuilder();

        var screenRows = Enumerable.Range(0, shape.Rows)
            .Select(row => Enumerable.Range(0, shape.Columns).Select(column => camera.Surface(row, column)).ToArray())
            .ToArray();

        var routePieces = BuildRoutePieces(shape, route, css);

        markup.Append($"<path class=\"floor\" clip-path=\"url(#table)\" d=\"{TableGrid(camera)}\"/>\n");
        AppendRulerAndMonths(markup, shape, camera, route);
        markup.Append("<g clip-path=\"url(#window)\">");
        for (var row = 0; row < shape.Rows; row++)
            markup.Append(RowGroup(shape, camera, palette, screenRows, row, routePieces[row]));
        markup.Append("</g>\n");
        markup.Append($"<path class=\"face\" d=\"{FrontFace(camera, screenRows)}\"/>\n");
        markup.Append($"<path class=\"face\" d=\"{SideFace(camera, screenRows)}\"/>\n");
        AppendNowMarker(markup, route.End, nowLabel);

        AppendAnimationRules(css, palette);
        AppendFlightKeyframes(css, shape, camera);
        AppendCarrierKeyframes(css, shape, route, screenRows);

        var floorCenter = camera.Project(0.1, 0, 0.5);
        var defs =
            $"<linearGradient id=\"fog\" gradientUnits=\"userSpaceOnUse\" x1=\"0\" y1=\"{Num.Format(TerrainCamera.BoxTop)}\" x2=\"0\" y2=\"{Num.Format(TerrainCamera.BoxBottom)}\">" +
            $"<stop offset=\"0\" stop-color=\"{palette.Fog}\"/><stop offset=\"1\" stop-color=\"{palette.Bg}\"/></linearGradient>" +
            $"<radialGradient id=\"floorfade\" gradientUnits=\"userSpaceOnUse\" cx=\"{Num.Format(floorCenter.X)}\" cy=\"{Num.Format(floorCenter.Y)}\" r=\"430\">" +
            $"<stop offset=\"0\" stop-color=\"{palette.Floor}\"/><stop offset=\".55\" stop-color=\"{palette.Line}\"/><stop offset=\"1\" stop-color=\"{palette.Bg}\"/></radialGradient>";

        return new TerrainSlots(defs, markup.ToString().TrimEnd(), css.ToString().TrimEnd());
    }

    private static string RowGroup(
        TerrainShape shape, TerrainCamera camera, Palette palette, ScreenPoint[][] screenRows, int row, string routePieces)
    {
        var points = screenRows[row];
        var groundY = camera.GroundY(row);
        var depthShare = (double)row / (shape.Rows - 1);
        var lineColor = Mix(palette.Far, palette.Near, Math.Pow(depthShare, 1.1));
        var isDayRow = TerrainShape.IsDayRow(row);

        var outline = new StringBuilder($"M{Num.Format(points[0].X)} {Num.Format(groundY)}");
        foreach (var point in points)
            outline.Append($"L{Num.Format(point.X)} {Num.Format(point.Y)}");
        outline.Append($"L{Num.Format(points[^1].X)} {Num.Format(groundY)}Z");

        var delays = string.Join(',',
            CssTime.Ms(DrawAt + row * DrawStep),
            CssTime.Ms(LiftAt + (shape.Rows - 1 - row) * LiftStep),
            CssTime.Ms(ScanAt + row * ScanStep));
        var stroke = isDayRow ? lineColor : Mix(palette.Bg, lineColor, 0.42);
        var group = new StringBuilder($"<g class=\"cam\" style=\"animation-name:cam{row}\">");
        group.Append($"<path class=\"row\" d=\"{outline}\" pathLength=\"1\" stroke-width=\"{(isDayRow ? "1.2" : ".7")}\" ")
             .Append($"style=\"--c:{stroke};transform-origin:0 {Num.Format(groundY)}px;animation-delay:{delays}\"/>");

        if (row > 0)
        {
            var grid = new StringBuilder();
            for (var column = 0; column < shape.Columns; column += TerrainShape.SamplesPerWeek)
            {
                var back = screenRows[row - 1][column];
                grid.Append($"M{Num.Format(back.X)} {Num.Format(back.Y)}L{Num.Format(points[column].X)} {Num.Format(points[column].Y)}");
            }

            var gridDelay = CssTime.Ms(Survey + 0.1 + (shape.Rows - 1 - row) * SurveyGridStep);
            group.Append($"<path class=\"xl\" d=\"{grid}\" stroke=\"{Mix(palette.Bg, lineColor, 0.62)}\" style=\"animation-delay:{gridDelay}\"/>");
        }

        return group.Append(routePieces).Append("</g>").ToString();
    }

    private static string[] BuildRoutePieces(TerrainShape shape, TerrainRoute route, StringBuilder css)
    {
        var pieces = Enumerable.Repeat(string.Empty, shape.Rows).ToArray();
        int LayerOf(int column) =>
            Math.Clamp((int)Math.Floor((route.Depths[column] + route.Depths[column + 1]) / 2), 0, shape.Rows - 1);

        var pieceNumber = 0;
        for (var start = 0; start < shape.Columns - 1;)
        {
            var layer = LayerOf(start);
            var end = start;
            while (end < shape.Columns - 1 && LayerOf(end) == layer)
                end++;

            var path = new StringBuilder();
            for (var column = start; column <= end; column++)
                path.Append($"{(column == start ? 'M' : 'L')}{Num.Format(route.Points[column].X)} {Num.Format(route.Points[column].Y)}");

            var startsAt = RouteEase.TimeAt(route.LengthAt(start) / route.Length) * RouteSeconds;
            var endsAt = RouteEase.TimeAt(route.LengthAt(end) / route.Length) * RouteSeconds;
            var timing = $"animation-duration:{CssTime.Ms(Math.Max(0.001, endsAt - startsAt))};animation-delay:{CssTime.Ms(RouteAt + startsAt)}";

            var phase = route.LengthAt(start) % FlowSpacing;
            css.Append($"@keyframes fl{pieceNumber}{{from{{stroke-dashoffset:{Num.Format(phase)}}}to{{stroke-dashoffset:{Num.Format(phase - FlowSpacing)}}}}}\n");

            pieces[layer] +=
                $"<path class=\"glow\" d=\"{path}\" pathLength=\"1\" style=\"{timing}\"/>" +
                $"<path class=\"route\" d=\"{path}\" pathLength=\"1\" style=\"{timing}\"/>" +
                $"<path class=\"flow\" d=\"{path}\" style=\"stroke-dashoffset:{Num.Format(phase)};animation-name:fl{pieceNumber},flowshow\"/>";

            pieceNumber++;
            start = end;
        }

        return pieces;
    }

    private static string TableGrid(TerrainCamera camera)
    {
        var grid = new StringBuilder();

        for (var step = (int)Math.Ceiling(TableFront * 6); step <= (int)Math.Floor(TableBack * 6); step++)
        {
            var z = step / 6.0;
            var (from, to) = (camera.Project(TableLeft, 0, z), camera.Project(TableRight, 0, z));
            grid.Append($"M{Num.Format(from.X)} {Num.Format(from.Y)}H{Num.Format(to.X)}");
        }

        var spacing = 2.0 / (ContributionYear.WeekCount - 1) * 4;
        for (var step = (int)Math.Ceiling((TableLeft + 1) / spacing); step <= (int)Math.Floor((TableRight + 1) / spacing); step++)
        {
            var x = -1 + step * spacing;
            var (from, to) = (camera.Project(x, 0, TableFront), camera.Project(x, 0, TableBack));
            grid.Append($"M{Num.Format(from.X)} {Num.Format(from.Y)}L{Num.Format(to.X)} {Num.Format(to.Y)}");
        }

        return grid.ToString();
    }

    private static void AppendRulerAndMonths(StringBuilder markup, TerrainShape shape, TerrainCamera camera, TerrainRoute route)
    {
        var ruler = new StringBuilder();
        var lit = new StringBuilder();
        var months = new StringBuilder();

        for (var week = 0; week < shape.WeekStarts.Count; week++)
        {
            var month = shape.WeekStarts[week].Month;
            var startsMonth = week > 0 && month != shape.WeekStarts[week - 1].Month;
            var column = week * TerrainShape.SamplesPerWeek;
            var foot = camera.Project(camera.X(column), 0, 0);

            ruler.Append($"M{Num.Format(foot.X)} {Num.Format(foot.Y + 3)}v{(startsMonth ? "7" : "3.5")}");
            if (!startsMonth)
                continue;

            var passes = CssTime.Ms(RouteAt + RouteEase.TimeAt(route.LengthAt(column) / route.Length) * RouteSeconds);
            months.Append($"<text x=\"{Num.Format(foot.X)}\" y=\"{Num.Format(foot.Y + 26)}\" style=\"animation-delay:{CssTime.Ms(Survey + 0.5)},{passes}\">{MonthLetters[month - 1]}</text>");
            lit.Append($"<path class=\"lit\" d=\"M{Num.Format(foot.X)} {Num.Format(foot.Y + 3)}v7\" style=\"animation-delay:{passes}\"/>");
        }

        markup.Append($"<path class=\"ruler\" d=\"{ruler}\"/>\n").Append(lit).Append('\n');
        markup.Append($"<g class=\"mo\">{months}</g>\n");
    }

    private static string FrontFace(TerrainCamera camera, ScreenPoint[][] screenRows)
    {
        var front = screenRows[^1];
        var groundY = Num.Format(camera.GroundY(screenRows.Length - 1));
        var path = new StringBuilder($"M{Num.Format(front[0].X)} {groundY}");
        foreach (var point in front)
            path.Append($"L{Num.Format(point.X)} {Num.Format(point.Y)}");
        return path.Append($"L{Num.Format(front[^1].X)} {groundY}Z").ToString();
    }

    private static string SideFace(TerrainCamera camera, ScreenPoint[][] screenRows)
    {
        var path = new StringBuilder();
        for (var row = screenRows.Length - 1; row >= 0; row--)
        {
            var point = screenRows[row][^1];
            path.Append($"{(path.Length == 0 ? 'M' : 'L')}{Num.Format(point.X)} {Num.Format(point.Y)}");
        }

        var (back, front) = (camera.Project(1, 0, 1), camera.Project(1, 0, 0));
        return path.Append($"L{Num.Format(back.X)} {Num.Format(back.Y)}L{Num.Format(front.X)} {Num.Format(front.Y)}Z").ToString();
    }

    private static void AppendNowMarker(StringBuilder markup, ScreenPoint now, string nowLabel)
    {
        var (x, y) = (Num.Format(now.X), Num.Format(now.Y));
        var top = Num.Format(now.Y - PoleHeight);
        markup.Append("<circle class=\"carrier\" cx=\"0\" cy=\"0\" r=\"4\"/>\n")
              .Append($"<path class=\"pole\" d=\"M{x} {y}V{top}\" pathLength=\"1\"/>\n")
              .Append($"<path class=\"flag\" d=\"M{x} {top}h13l-4 4.5 4 4.5h-13z\"/>\n")
              .Append($"<circle class=\"ring\" cx=\"{x}\" cy=\"{y}\" r=\"18\"/>\n")
              .Append($"<circle class=\"beacon\" cx=\"{x}\" cy=\"{y}\" r=\"18\"/>\n")
              .Append($"<circle class=\"now\" cx=\"{x}\" cy=\"{y}\" r=\"4.5\"/>\n")
              .Append($"<text class=\"tag\" x=\"{Num.Format(now.X + 18)}\" y=\"{Num.Format(now.Y - PoleHeight + 8)}\">{nowLabel}</text>");
    }

    private static void AppendAnimationRules(StringBuilder css, Palette palette)
    {
        var flowTotal = FlowPeriod * FlowCycles;
        css.Append($".hdr{{animation:fade {CssTime.Ms(HeaderSeconds)} ease-out {CssTime.Ms(HeaderAt)} both}}\n")
           .Append($".role{{animation:rise {CssTime.Ms(RoleSeconds)} cubic-bezier(.2,.7,.2,1) {CssTime.Ms(RoleAt)} both}}\n")
           .Append($".cam{{animation-duration:{CssTime.Ms(FlightSeconds)};animation-timing-function:linear;animation-delay:{CssTime.Ms(FlightAt)};animation-fill-mode:both}}\n")
           .Append($".floor{{animation:fade 1400ms ease-out {CssTime.Ms(Survey - 0.2)} both}}\n")
           .Append($".row{{animation:draw {CssTime.Ms(DrawSeconds)} cubic-bezier(.5,0,.2,1) both,lift {CssTime.Ms(LiftSeconds)} cubic-bezier(.2,.75,.15,1) both,scan {CssTime.Ms(ScanCycle)} linear {ScanCycles}}}\n")
           .Append(".xl{animation:fade 700ms ease-out both}\n")
           .Append($".face{{animation:fade 800ms ease-out {CssTime.Ms(Survey + 0.3)} both}}\n")
           .Append($".mo text{{animation:fade 800ms ease-out both,lit {CssTime.Ms(MonthLightSeconds)} ease-out}}\n")
           .Append($".ruler{{animation:fade 800ms ease-out {CssTime.Ms(Survey + 0.5)} both}}\n")
           .Append($".lit{{animation:litline {CssTime.Ms(MonthLightSeconds)} ease-out}}\n")
           .Append(".route,.glow{animation-name:draw,appear;animation-timing-function:linear;animation-fill-mode:both}\n")
           .Append($".flow{{stroke-dasharray:{Num.Format(FlowDash)} {Num.Format(FlowSpacing - FlowDash)};animation-duration:{CssTime.Ms(FlowPeriod)},{CssTime.Ms(flowTotal)};")
           .Append($"animation-timing-function:linear;animation-delay:{CssTime.Ms(FlowAt)};animation-iteration-count:{FlowCycles},1}}\n")
           .Append($".carrier{{animation:carry {CssTime.Ms(RouteSeconds)} linear {CssTime.Ms(RouteAt)} forwards}}\n")
           .Append($".pole{{animation:draw 500ms cubic-bezier(.2,.7,.2,1) {CssTime.Ms(Arrival + 0.15)} both}}\n")
           .Append($".flag{{animation:unfurl 500ms cubic-bezier(.2,.8,.2,1.3) {CssTime.Ms(Arrival + 0.55)} both,")
           .Append($"wave {CssTime.Ms(FlagWaveSeconds)} ease-in-out {CssTime.Ms(FlagWaveAt)} {FlagWaves} alternate}}\n")
           .Append($".now{{animation:pop 500ms cubic-bezier(.2,.8,.2,1.4) {CssTime.Ms(Arrival)} both}}\n")
           .Append($".ring{{animation:ping 1400ms cubic-bezier(.2,0,0,1) {CssTime.Ms(Arrival)} forwards}}\n")
           .Append($".beacon{{animation:beacon {CssTime.Ms(BeaconCycle)} cubic-bezier(.2,0,0,1) {CssTime.Ms(BeaconAt)} {BeaconCycles}}}\n")
           .Append($"text.tag{{animation:fade 500ms ease-out {CssTime.Ms(Arrival + 0.7)} both}}\n")
           .Append("@keyframes lift{from{transform:scaleY(.03)}}\n")
           .Append($"@keyframes scan{{0%,100%{{stroke:var(--c)}}5%{{stroke:{palette.Ink}}}11%{{stroke:var(--c)}}}}\n")
           .Append($"@keyframes lit{{from{{fill:{palette.Signal}}}}}\n")
           .Append("@keyframes litline{from{opacity:1}}\n")
           .Append("@keyframes appear{from{visibility:hidden}to{visibility:visible}}\n")
           .Append("@keyframes flowshow{0%,97%{opacity:.95}100%{opacity:0}}\n")
           .Append("@keyframes pop{from{transform:scale(0)}}\n")
           .Append("@keyframes ping{from{opacity:.9;transform:scale(.25)}to{opacity:0;transform:scale(2.2)}}\n")
           .Append("@keyframes beacon{0%{opacity:.7;transform:scale(.3)}45%,100%{opacity:0;transform:scale(2.6)}}\n")
           .Append("@keyframes unfurl{from{transform:scaleX(0)}}\n")
           .Append("@keyframes wave{to{transform:skewY(-9deg) scaleX(.86)}}\n");
    }

    private static void AppendFlightKeyframes(StringBuilder css, TerrainShape shape, TerrainCamera camera)
    {
        for (var row = 0; row < shape.Rows; row++)
        {
            css.Append($"@keyframes cam{row}{{");
            for (var frame = 0; frame < FlightFrames; frame++)
            {
                var (scale, dx, dy) = camera.PlaneSeenFrom(camera.Z(row), FlightCamera((double)frame / FlightFrames));
                var s = Num.Format(scale, 3);
                css.Append($"{Num.Percent((double)frame / FlightFrames)}{{transform:matrix({s},0,0,{s},{Num.Format(dx)},{Num.Format(dy)})}}");
            }

            css.Append("100%{transform:none}}\n");
        }
    }

    private static CameraPosition FlightCamera(double time)
    {
        var progress = FlightEase.Progress(time);
        var final = TerrainCamera.Final;
        return new CameraPosition(
            Lerp(FlightStart.X, final.X, progress),
            Lerp(FlightStart.Y, final.Y, Math.Pow(progress, 0.7)),
            Lerp(FlightStart.Distance, final.Distance, Math.Pow(progress, 1.25)));
    }

    private static void AppendCarrierKeyframes(StringBuilder css, TerrainShape shape, TerrainRoute route, ScreenPoint[][] screenRows)
    {
        css.Append("@keyframes carry{");
        for (var step = 0; step <= CarrierSteps; step++)
        {
            var (point, depth) = route.At(RouteEase.Progress((double)step / CarrierSteps) * route.Length);
            var hidden = false;
            for (var row = (int)Math.Floor(depth) + 1; row < shape.Rows && !hidden; row++)
                hidden = RidgeY(screenRows[row], point.X) < point.Y - 2;

            var opacity = step == CarrierSteps || hidden ? 0 : 1;
            css.Append($"{Num.Percent((double)step / CarrierSteps)}{{opacity:{opacity};transform:translate({Num.Format(point.X)}px,{Num.Format(point.Y)}px)}}");
        }

        css.Append("}\n");
    }

    private static double RidgeY(ScreenPoint[] points, double x)
    {
        if (x < points[0].X || x > points[^1].X)
            return double.PositiveInfinity;

        var index = 1;
        while (index < points.Length - 1 && points[index].X < x)
            index++;

        var span = points[index].X - points[index - 1].X;
        var t = span > 0 ? (x - points[index - 1].X) / span : 0;
        return points[index - 1].Y + (points[index].Y - points[index - 1].Y) * t;
    }

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;

    private static string Mix(string from, string to, double t)
    {
        static int Channel(string hex, int index) => int.Parse(hex.AsSpan(1 + index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        var result = new StringBuilder("#");
        for (var index = 0; index < 3; index++)
        {
            var value = (int)Math.Round(Channel(from, index) * (1 - t) + Channel(to, index) * t, MidpointRounding.AwayFromZero);
            result.Append(value.ToString("X2", CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }
}
