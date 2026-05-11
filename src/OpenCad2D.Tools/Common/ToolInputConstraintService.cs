using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Applies cross-tool input constraints such as Ortho mode and polar tracking.
/// </summary>
public static class ToolInputConstraintService
{
    /// <summary>
    /// Applies the effective angular constraint for an interactive tool.
    /// Polar tracking has priority when enabled; otherwise legacy Ortho constrains to 90°.
    /// </summary>
    public static Point2D ApplyAngleConstraint(
        ToolContext context,
        Point2D basePoint,
        Point2D currentPoint)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.AngleConstraintSettings.IsEnabled)
        {
            return AngleConstraintService.Apply(
                basePoint,
                currentPoint,
                context.AngleConstraintSettings);
        }

        return ApplyOrtho(
            context.IsOrthoEnabled,
            basePoint,
            currentPoint);
    }

    public static Point2D ApplyOrtho(
        bool isOrthoEnabled,
        Point2D basePoint,
        Point2D currentPoint)
    {
        if (!isOrthoEnabled)
        {
            return currentPoint;
        }

        double dx = currentPoint.X - basePoint.X;
        double dy = currentPoint.Y - basePoint.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return new Point2D(
                currentPoint.X,
                basePoint.Y);
        }

        return new Point2D(
            basePoint.X,
            currentPoint.Y);
    }
}
