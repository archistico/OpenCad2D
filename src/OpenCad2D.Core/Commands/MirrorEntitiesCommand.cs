using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Mirrors entities across an infinite mirror line.
/// </summary>
public sealed class MirrorEntitiesCommand : TransformEntitiesCommand
{
    public MirrorEntitiesCommand(
        IEnumerable<EntityId> entityIds,
        Line2D mirrorLine)
        : base(
            entityIds,
            Matrix2D.Mirror(mirrorLine))
    {
        MirrorLine = mirrorLine;
    }

    public Line2D MirrorLine { get; }

    public override string Name => "Mirror entities";
}