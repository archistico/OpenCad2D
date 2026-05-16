using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides grips for ellipse entities.
/// </summary>
public sealed class EllipseGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is EllipseEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        EllipseEntity ellipse = GetEllipse(entity);

        return new[]
        {
            new GripPoint(
                ellipse.Center,
                GripKind.MoveEntity,
                ellipse.Id,
                0),

            new GripPoint(
                ellipse.MajorAxisEndPoint,
                GripKind.ResizeRadius,
                ellipse.Id,
                1),

            new GripPoint(
                ellipse.MinorAxisEndPoint,
                GripKind.ResizeRadius,
                ellipse.Id,
                2),

            new GripPoint(
                ellipse.MajorAxisStartPoint,
                GripKind.ResizeRadius,
                ellipse.Id,
                3),

            new GripPoint(
                ellipse.MinorAxisStartPoint,
                GripKind.ResizeRadius,
                ellipse.Id,
                4)
        };
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        EllipseEntity ellipse = GetEllipse(entity);

        if (gripIndex == 0)
        {
            return new EllipseEntity(
                destination,
                ellipse.MajorAxis,
                ellipse.MinorRadius,
                ellipse.Id,
                ellipse.LayerId,
                ellipse.Style,
                ellipse.IsVisible,
                ellipse.IsLocked,
                ellipse.DrawOrder);
        }

        if (gripIndex is 1 or 3)
        {
            Vector2D majorAxis = ellipse.Center.VectorTo(destination);
            if (gripIndex == 3)
            {
                majorAxis = majorAxis * -1.0;
            }

            return new EllipseEntity(
                ellipse.Center,
                majorAxis,
                ellipse.MinorRadius,
                ellipse.Id,
                ellipse.LayerId,
                ellipse.Style,
                ellipse.IsVisible,
                ellipse.IsLocked,
                ellipse.DrawOrder);
        }

        if (gripIndex is 2 or 4)
        {
            double minorRadius = ellipse.Center.DistanceTo(destination);

            return new EllipseEntity(
                ellipse.Center,
                ellipse.MajorAxis,
                minorRadius,
                ellipse.Id,
                ellipse.LayerId,
                ellipse.Style,
                ellipse.IsVisible,
                ellipse.IsLocked,
                ellipse.DrawOrder);
        }

        throw new ArgumentOutOfRangeException(
            nameof(gripIndex),
            gripIndex,
            "Unknown ellipse grip index.");
    }

    private static EllipseEntity GetEllipse(CadEntity entity)
    {
        return entity as EllipseEntity
            ?? throw new ArgumentException(
                "Entity must be an ellipse entity.",
                nameof(entity));
    }
}
