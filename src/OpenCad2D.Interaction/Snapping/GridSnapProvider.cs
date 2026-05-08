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

        double snappedX = SnapCoordinate(
            request.CursorPoint.X,
            grid.OriginX,
            grid.Step);

        double snappedY = SnapCoordinate(
            request.CursorPoint.Y,
            grid.OriginY,
            grid.Step);

        var point = new Point2D(
            snappedX,
            snappedY);

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