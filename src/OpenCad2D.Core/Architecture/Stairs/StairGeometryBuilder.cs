using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Architecture.Stairs;

/// <summary>
/// Generates conservative 2D linework from parametric stair settings.
/// </summary>
public static class StairGeometryBuilder
{
    private const double ArrowHeadAngleDegrees = 30.0;
    private const double SectionMarkerAngleDegrees = 30.0;

    public static StairGeometry Build(StairEntity stair)
    {
        ArgumentNullException.ThrowIfNull(stair);

        return stair.ViewKind switch
        {
            StairViewKind.Plan => BuildPlan(stair),
            StairViewKind.SideElevation => new StairGeometry(BuildSideElevation(stair)),
            StairViewKind.FrontElevation => new StairGeometry(BuildFrontElevation(stair)),
            _ => throw new InvalidOperationException($"Unsupported stair view '{stair.ViewKind}'.")
        };
    }

    private static StairGeometry BuildPlan(StairEntity stair)
    {
        double run = stair.TotalRun;
        double width = stair.Width;

        var primarySegments = new List<LineSegment2D>
        {
            Segment(stair, 0, 0, run, 0),
            Segment(stair, run, 0, run, width),
            Segment(stair, run, width, 0, width),
            Segment(stair, 0, width, 0, 0)
        };

        for (int index = 1; index < stair.TreadCount; index++)
        {
            double x = index * stair.TreadDepth;
            primarySegments.Add(Segment(stair, x, 0, x, width));
        }

        var annotationSegments = new List<LineSegment2D>();
        AddDirectionArrow(stair, annotationSegments);
        AddSectionMarker(stair, annotationSegments);

        return new StairGeometry(primarySegments, annotationSegments);
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

    private static void AddDirectionArrow(
        StairEntity stair,
        List<LineSegment2D> annotationSegments)
    {
        if (stair.PlanArrowMode == StairPlanArrowMode.None)
        {
            return;
        }

        double run = stair.TotalRun;
        double halfWidth = stair.Width / 2.0;

        double startX = stair.PlanArrowMode == StairPlanArrowMode.FirstToLast
            ? 0.0
            : run;
        double endX = stair.PlanArrowMode == StairPlanArrowMode.FirstToLast
            ? run
            : 0.0;

        AddSegmentIfVisible(annotationSegments, Segment(stair, startX, halfWidth, endX, halfWidth));

        double sign = stair.PlanArrowMode == StairPlanArrowMode.FirstToLast
            ? 1.0
            : -1.0;
        double arrowHeadLength = Math.Min(
            Math.Min(stair.Width * 0.20, stair.TreadDepth),
            run * 0.20);

        if (arrowHeadLength <= 0.0)
        {
            return;
        }

        double angle = ArrowHeadAngleDegrees * Math.PI / 180.0;
        double backX = endX - sign * arrowHeadLength * Math.Cos(angle);
        double offsetY = arrowHeadLength * Math.Sin(angle);

        AddSegmentIfVisible(annotationSegments, Segment(stair, endX, halfWidth, backX, halfWidth + offsetY));
        AddSegmentIfVisible(annotationSegments, Segment(stair, endX, halfWidth, backX, halfWidth - offsetY));
    }

    private static void AddSectionMarker(
        StairEntity stair,
        List<LineSegment2D> annotationSegments)
    {
        if (!stair.ShowPlanSectionMarker || stair.SlabThickness <= 0.0)
        {
            return;
        }

        double angle = SectionMarkerAngleDegrees * Math.PI / 180.0;
        double directionX = Math.Cos(angle);
        double directionY = Math.Sin(angle);
        double normalX = -directionY;
        double normalY = directionX;

        double centerX = stair.TotalRun / 2.0;
        double centerY = stair.Width / 2.0;
        double halfDistance = stair.SlabThickness / 2.0;
        double halfLength = stair.Width / (2.0 * Math.Abs(directionY));

        AddSectionMarkerLine(
            stair,
            annotationSegments,
            centerX + normalX * halfDistance,
            centerY + normalY * halfDistance,
            directionX,
            directionY,
            halfLength);

        AddSectionMarkerLine(
            stair,
            annotationSegments,
            centerX - normalX * halfDistance,
            centerY - normalY * halfDistance,
            directionX,
            directionY,
            halfLength);
    }

    private static void AddSectionMarkerLine(
        StairEntity stair,
        List<LineSegment2D> annotationSegments,
        double centerX,
        double centerY,
        double directionX,
        double directionY,
        double halfLength)
    {
        AddSegmentIfVisible(
            annotationSegments,
            Segment(
                stair,
                centerX - directionX * halfLength,
                centerY - directionY * halfLength,
                centerX + directionX * halfLength,
                centerY + directionY * halfLength));
    }

    private static void AddSegmentIfVisible(
        List<LineSegment2D> segments,
        LineSegment2D segment)
    {
        if (segment.Start.DistanceTo(segment.End) > 1e-12)
        {
            segments.Add(segment);
        }
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
