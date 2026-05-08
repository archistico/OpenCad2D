using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.HitTesting;

/// <summary>
/// Represents the result of a hit test operation.
/// </summary>
public sealed class HitTestResult
{
    public HitTestResult(
        CadEntity entity,
        Point2D closestPoint,
        double distance)
    {
        Entity = entity;
        ClosestPoint = closestPoint;
        Distance = distance;
    }

    public CadEntity Entity { get; }

    public Point2D ClosestPoint { get; }

    public double Distance { get; }
}