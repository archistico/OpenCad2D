using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Architecture.Stairs;

/// <summary>
/// Generates conservative 2D linework from parametric stair settings.
/// </summary>
public static class StairGeometryBuilder
{
    public static StairGeometry Build(StairEntity stair)
    {
        ArgumentNullException.ThrowIfNull(stair);

        List<LineSegment2D> segments = stair.ViewKind switch
        {
            StairViewKind.Plan => BuildPlan(stair),
            StairViewKind.SideElevation => BuildSideElevation(stair),
            StairViewKind.FrontElevation => BuildFrontElevation(stair),
            _ => throw new InvalidOperationException($"Unsupported stair view '{stair.ViewKind}'.")
        };

        return new StairGeometry(segments);
    }

    private static List<LineSegment2D> BuildPlan(StairEntity stair)
    {
        double run = stair.TotalRun;
        double width = stair.Width;

        var segments = new List<LineSegment2D>
        {
            Segment(stair, 0, 0, run, 0),
            Segment(stair, run, 0, run, width),
            Segment(stair, run, width, 0, width),
            Segment(stair, 0, width, 0, 0)
        };

        for (int index = 1; index < stair.TreadCount; index++)
        {
            double x = index * stair.TreadDepth;
            segments.Add(Segment(stair, x, 0, x, width));
        }

        return segments;
    }

    private static List<LineSegment2D> BuildSideElevation(StairEntity stair)
    {
        var segments = new List<LineSegment2D>();

        double x = 0.0;
        double y = 0.0;

        for (int index = 0; index < stair.TreadCount; index++)
        {
            double nextY = y + stair.RiserHeight;
            segments.Add(Segment(stair, x, y, x, nextY));
            y = nextY;

            double nextX = x + stair.TreadDepth;
            segments.Add(Segment(stair, x, y, nextX, y));
            x = nextX;
        }

        if (stair.ShowStructure && stair.SlabThickness > 0.0)
        {
            segments.Add(Segment(
                stair,
                0.0,
                -stair.SlabThickness,
                stair.TotalRun,
                stair.TotalRise - stair.SlabThickness));
        }

        return segments;
    }

    private static List<LineSegment2D> BuildFrontElevation(StairEntity stair)
    {
        double width = stair.Width;
        double rise = stair.TotalRise;

        var segments = new List<LineSegment2D>
        {
            Segment(stair, 0, 0, width, 0),
            Segment(stair, width, 0, width, rise),
            Segment(stair, width, rise, 0, rise),
            Segment(stair, 0, rise, 0, 0)
        };

        for (int index = 1; index < stair.TreadCount; index++)
        {
            double y = index * stair.RiserHeight;
            segments.Add(Segment(stair, 0, y, width, y));
        }

        if (stair.ShowStructure && stair.SlabThickness > 0.0)
        {
            segments.Add(Segment(stair, 0, -stair.SlabThickness, width, -stair.SlabThickness));
        }

        return segments;
    }

    private static LineSegment2D Segment(
        StairEntity stair,
        double startX,
        double startY,
        double endX,
        double endY)
    {
        return new LineSegment2D(
            ToWorld(stair, startX, startY),
            ToWorld(stair, endX, endY));
    }

    private static Point2D ToWorld(StairEntity stair, double x, double y)
    {
        return stair.InsertionPoint
            + stair.XAxis * x
            + stair.YAxis * y;
    }
}
