using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides snap candidates on the nearest grid point.
/// </summary>
public sealed class GridSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Grid;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        GridSettings grid = request.GridSettings;

        Point2D point = grid.Kind switch
        {
            GridKind.Isometric => FindNearestIsometricPoint(request.CursorPoint, grid),
            _ => FindNearestRectangularPoint(request.CursorPoint, grid)
        };

        double distance = request.CursorPoint.DistanceTo(point);

        if (distance <= request.Tolerance)
        {
            yield return new SnapCandidate(
                SnapKind.Grid,
                point,
                entityId: null,
                distance);
        }
    }

    private static Point2D FindNearestRectangularPoint(
        Point2D cursorPoint,
        GridSettings grid)
    {
        double snappedX = SnapCoordinate(
            cursorPoint.X,
            grid.OriginX,
            grid.Step);

        double snappedY = SnapCoordinate(
            cursorPoint.Y,
            grid.OriginY,
            grid.Step);

        return new Point2D(
            snappedX,
            snappedY);
    }

    private static Point2D FindNearestIsometricPoint(
        Point2D cursorPoint,
        GridSettings grid)
    {
        double angleRadians = grid.IsometricAngleDegrees * Math.PI / 180.0;
        double tangent = Math.Tan(angleRadians);
        double step = grid.Step;
        double verticalStep = grid.GetIsometricVerticalStep(step);

        Point2D best = cursorPoint;
        double bestDistance = double.MaxValue;

        double relativeX = cursorPoint.X - grid.OriginX;
        int verticalIndex = (int)Math.Round(relativeX / verticalStep, MidpointRounding.AwayFromZero);

        double positiveIntercept = cursorPoint.Y - grid.OriginY - tangent * relativeX;
        int positiveIndex = (int)Math.Round(positiveIntercept / step, MidpointRounding.AwayFromZero);

        double negativeIntercept = cursorPoint.Y - grid.OriginY + tangent * relativeX;
        int negativeIndex = (int)Math.Round(negativeIntercept / step, MidpointRounding.AwayFromZero);

        for (int n = verticalIndex - 2; n <= verticalIndex + 2; n++)
        {
            for (int k = positiveIndex - 2; k <= positiveIndex + 2; k++)
            {
                ConsiderCandidate(
                    new Point2D(
                        grid.OriginX + n * verticalStep,
                        grid.OriginY + tangent * n * verticalStep + k * step),
                    cursorPoint,
                    ref best,
                    ref bestDistance);
            }

            for (int k = negativeIndex - 2; k <= negativeIndex + 2; k++)
            {
                ConsiderCandidate(
                    new Point2D(
                        grid.OriginX + n * verticalStep,
                        grid.OriginY - tangent * n * verticalStep + k * step),
                    cursorPoint,
                    ref best,
                    ref bestDistance);
            }
        }

        for (int p = positiveIndex - 2; p <= positiveIndex + 2; p++)
        {
            for (int q = negativeIndex - 2; q <= negativeIndex + 2; q++)
            {
                double x = grid.OriginX + (q - p) * step / (2 * tangent);
                double y = grid.OriginY + (p + q) * step / 2;

                ConsiderCandidate(
                    new Point2D(x, y),
                    cursorPoint,
                    ref best,
                    ref bestDistance);
            }
        }

        return best;
    }

    private static void ConsiderCandidate(
        Point2D candidate,
        Point2D cursorPoint,
        ref Point2D best,
        ref double bestDistance)
    {
        double distance = cursorPoint.DistanceTo(candidate);

        if (distance >= bestDistance)
        {
            return;
        }

        best = candidate;
        bestDistance = distance;
    }

    private static double SnapCoordinate(
        double value,
        double origin,
        double step)
    {
        double relative = value - origin;
        double rounded = Math.Round(
            relative / step,
            MidpointRounding.AwayFromZero);

        return origin + rounded * step;
    }
}
