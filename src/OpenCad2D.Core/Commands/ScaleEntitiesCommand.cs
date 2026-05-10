using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Uniformly scales entities around a base point.
/// </summary>
public sealed class ScaleEntitiesCommand : TransformEntitiesCommand
{
    public ScaleEntitiesCommand(
        IEnumerable<EntityId> entityIds,
        Point2D center,
        double factor)
        : base(
            entityIds,
            Matrix2D.Scale(factor, center))
    {
        if (factor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(factor),
                "Scale factor must be greater than zero.");
        }

        Center = center;
        Factor = factor;
    }

    public Point2D Center { get; }

    public double Factor { get; }

    public override string Name => "Scale entities";
}
