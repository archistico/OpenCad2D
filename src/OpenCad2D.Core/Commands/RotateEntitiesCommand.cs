using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Rotates entities around a base point.
/// </summary>
public sealed class RotateEntitiesCommand : TransformEntitiesCommand
{
    public RotateEntitiesCommand(
        IEnumerable<EntityId> entityIds,
        Point2D center,
        Angle angle)
        : base(
            entityIds,
            Matrix2D.Rotation(angle.Radians, center))
    {
        Center = center;
        Angle = angle;
    }

    public Point2D Center { get; }

    public Angle Angle { get; }

    public override string Name => "Rotate entities";
}