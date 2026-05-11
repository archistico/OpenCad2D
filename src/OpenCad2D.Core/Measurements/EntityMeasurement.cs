using OpenCad2D.Core.Entities;

namespace OpenCad2D.Core.Measurements;

/// <summary>
/// Describes the measurable values of one CAD entity.
/// Values that do not apply to the measured entity are null.
/// </summary>
public sealed class EntityMeasurement
{
    public EntityMeasurement(
        EntityKind entityKind,
        double? length = null,
        double? angleDegrees = null,
        double? radius = null,
        double? diameter = null,
        double? circumference = null,
        double? area = null,
        double? sweepAngleDegrees = null,
        int? vertexCount = null,
        bool? isClosed = null)
    {
        EntityKind = entityKind;
        Length = length;
        AngleDegrees = angleDegrees;
        Radius = radius;
        Diameter = diameter;
        Circumference = circumference;
        Area = area;
        SweepAngleDegrees = sweepAngleDegrees;
        VertexCount = vertexCount;
        IsClosed = isClosed;
    }

    public EntityKind EntityKind { get; }

    public double? Length { get; }

    public double? AngleDegrees { get; }

    public double? Radius { get; }

    public double? Diameter { get; }

    public double? Circumference { get; }

    public double? Area { get; }

    public double? SweepAngleDegrees { get; }

    public int? VertexCount { get; }

    public bool? IsClosed { get; }
}
