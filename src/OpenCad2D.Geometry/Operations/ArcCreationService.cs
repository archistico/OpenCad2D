using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Geometry.Operations;

/// <summary>
/// Provides construction helpers for circular arcs.
/// </summary>
public static class ArcCreationService
{
    /// <summary>
    /// Attempts to create the circular arc that starts at <paramref name="startPoint"/>,
    /// passes through <paramref name="pointOnArc"/> and ends at <paramref name="endPoint"/>.
    /// </summary>
    public static bool TryCreateFromThreePoints(
        Point2D startPoint,
        Point2D pointOnArc,
        Point2D endPoint,
        out Arc2D arc)
    {
        return TryCreateFromThreePoints(
            startPoint,
            pointOnArc,
            endPoint,
            GeometryTolerance.Default,
            out arc);
    }

    /// <summary>
    /// Attempts to create the circular arc that starts at <paramref name="startPoint"/>,
    /// passes through <paramref name="pointOnArc"/> and ends at <paramref name="endPoint"/>.
    /// </summary>
    public static bool TryCreateFromThreePoints(
        Point2D startPoint,
        Point2D pointOnArc,
        Point2D endPoint,
        GeometryTolerance tolerance,
        out Arc2D arc)
    {
        arc = default;

        if (tolerance.ArePointsEqual(startPoint, pointOnArc) ||
            tolerance.ArePointsEqual(pointOnArc, endPoint) ||
            tolerance.ArePointsEqual(startPoint, endPoint))
        {
            return false;
        }

        double determinant = 2.0 *
            (startPoint.X * (pointOnArc.Y - endPoint.Y) +
             pointOnArc.X * (endPoint.Y - startPoint.Y) +
             endPoint.X * (startPoint.Y - pointOnArc.Y));

        if (tolerance.IsDistanceZero(determinant))
        {
            return false;
        }

        double startSquared = startPoint.X * startPoint.X +
            startPoint.Y * startPoint.Y;
        double midSquared = pointOnArc.X * pointOnArc.X +
            pointOnArc.Y * pointOnArc.Y;
        double endSquared = endPoint.X * endPoint.X +
            endPoint.Y * endPoint.Y;

        double centerX =
            (startSquared * (pointOnArc.Y - endPoint.Y) +
             midSquared * (endPoint.Y - startPoint.Y) +
             endSquared * (startPoint.Y - pointOnArc.Y)) /
            determinant;

        double centerY =
            (startSquared * (endPoint.X - pointOnArc.X) +
             midSquared * (startPoint.X - endPoint.X) +
             endSquared * (pointOnArc.X - startPoint.X)) /
            determinant;

        var center = new Point2D(centerX, centerY);
        double radius = center.DistanceTo(startPoint);

        if (tolerance.IsDistanceZero(radius))
        {
            return false;
        }

        Angle startAngle = GetAngle(center, startPoint);
        Angle midAngle = GetAngle(center, pointOnArc);
        Angle endAngle = GetAngle(center, endPoint);

        bool isCounterClockwise = IsAngleOnCounterClockwiseArc(
            startAngle,
            endAngle,
            midAngle,
            tolerance.Angle);

        arc = new Arc2D(
            center,
            radius,
            startAngle,
            endAngle,
            isCounterClockwise);

        return true;
    }

    private static Angle GetAngle(
        Point2D center,
        Point2D point)
    {
        return Angle.FromRadians(
            Math.Atan2(
                point.Y - center.Y,
                point.X - center.X));
    }

    private static bool IsAngleOnCounterClockwiseArc(
        Angle startAngle,
        Angle endAngle,
        Angle candidateAngle,
        double tolerance)
    {
        double start = startAngle.NormalizePositive().Radians;
        double end = endAngle.NormalizePositive().Radians;
        double candidate = candidateAngle.NormalizePositive().Radians;

        if (end < start)
        {
            end += 2.0 * Math.PI;
        }

        if (candidate < start)
        {
            candidate += 2.0 * Math.PI;
        }

        return candidate >= start - tolerance &&
            candidate <= end + tolerance;
    }
}
