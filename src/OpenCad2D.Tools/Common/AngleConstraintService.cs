using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Applies polar tracking by constraining a candidate point to the nearest configured angular direction.
/// </summary>
public static class AngleConstraintService
{
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;

    /// <summary>
    /// Applies the configured angular constraint to a candidate point.
    /// </summary>
    /// <param name="basePoint">The point from which the direction is measured.</param>
    /// <param name="candidatePoint">The point already resolved by snapping or by the raw pointer position.</param>
    /// <param name="settings">The polar tracking settings.</param>
    /// <returns>
    /// The constrained point when polar tracking is enabled; otherwise <paramref name="candidatePoint"/>.
    /// </returns>
    public static Point2D Apply(
        Point2D basePoint,
        Point2D candidatePoint,
        AngleConstraintSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsEnabled)
        {
            return candidatePoint;
        }

        double dx = candidatePoint.X - basePoint.X;
        double dy = candidatePoint.Y - basePoint.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (Tolerance.IsZero(distance))
        {
            return candidatePoint;
        }

        double constrainedAngleDegrees = GetNearestAngleDegrees(
            Math.Atan2(dy, dx) * RadiansToDegrees,
            settings.StepDegrees);

        double constrainedAngleRadians = constrainedAngleDegrees * DegreesToRadians;

        return new Point2D(
            basePoint.X + Math.Cos(constrainedAngleRadians) * distance,
            basePoint.Y + Math.Sin(constrainedAngleRadians) * distance);
    }

    /// <summary>
    /// Rounds an angle to the nearest configured angular step.
    /// </summary>
    /// <param name="angleDegrees">The source angle, in degrees.</param>
    /// <param name="stepDegrees">The angular step, in degrees.</param>
    /// <returns>The nearest constrained angle, in degrees.</returns>
    public static double GetNearestAngleDegrees(
        double angleDegrees,
        double stepDegrees)
    {
        _ = AngleConstraintSettings.FromStep(stepDegrees);

        return Math.Round(
            angleDegrees / stepDegrees,
            MidpointRounding.AwayFromZero) * stepDegrees;
    }
}
