using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides the single move grip for point entities.
/// </summary>
public sealed class PointGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is PointEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        PointEntity point = GetPoint(entity);

        return new[]
        {
            new GripPoint(
                point.Position,
                GripKind.MoveEntity,
                point.Id,
                0)
        };
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        PointEntity point = GetPoint(entity);

        if (gripIndex != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown point grip index.");
        }

        return new PointEntity(
            destination,
            point.Id,
            point.LayerId,
            point.Style,
            point.IsVisible,
            point.IsLocked,
            point.DrawOrder);
    }

    private static PointEntity GetPoint(CadEntity entity)
    {
        return entity as PointEntity
            ?? throw new ArgumentException(
                "Entity must be a point entity.",
                nameof(entity));
    }
}
