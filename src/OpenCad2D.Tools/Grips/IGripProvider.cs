using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides grip points and grip-edit transformations for supported entity types.
/// </summary>
public interface IGripProvider
{
    bool CanHandle(CadEntity entity);

    IReadOnlyList<GripPoint> GetGrips(CadEntity entity);

    CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination);
}
