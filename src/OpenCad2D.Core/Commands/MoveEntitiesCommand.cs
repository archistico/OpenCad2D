using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Moves entities by a displacement vector.
/// </summary>
public sealed class MoveEntitiesCommand : TransformEntitiesCommand
{
    public MoveEntitiesCommand(
        IEnumerable<EntityId> entityIds,
        Vector2D displacement)
        : base(
            entityIds,
            Matrix2D.Translation(displacement.X, displacement.Y))
    {
        Displacement = displacement;
    }

    public Vector2D Displacement { get; }

    public override string Name => "Move entities";
}