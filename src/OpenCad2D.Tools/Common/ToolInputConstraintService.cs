using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Applies cross-tool input constraints such as Ortho mode.
/// </summary>
public static class ToolInputConstraintService
{
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
