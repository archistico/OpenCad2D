using OpenCad2D.Geometry.Coordinates;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Represents pointer input in world and user coordinates.
/// </summary>
public sealed class PointerInfo
{
    public PointerInfo(
        Point2D modelPoint,
        PointerModifiers modifiers = PointerModifiers.None)
        : this(
            modelPoint,
            CoordinateSystem2D.World.WorldToUser(modelPoint),
            modifiers)
    {
    }

    public PointerInfo(
        Point2D modelPoint,
        Point2D userPoint,
        PointerModifiers modifiers = PointerModifiers.None)
    {
        ModelPoint = modelPoint;
        UserPoint = userPoint;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Point in world/model coordinates.
    /// </summary>
    public Point2D ModelPoint { get; }

    /// <summary>
    /// Point in current user coordinate system.
    /// </summary>
    public Point2D UserPoint { get; }

    public PointerModifiers Modifiers { get; }

    public bool IsShiftPressed =>
        Modifiers.HasFlag(PointerModifiers.Shift);

    public bool IsControlPressed =>
        Modifiers.HasFlag(PointerModifiers.Control);

    public bool IsAltPressed =>
        Modifiers.HasFlag(PointerModifiers.Alt);
}