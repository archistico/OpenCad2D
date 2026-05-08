using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Represents pointer input in model coordinates.
/// </summary>
public sealed class PointerInfo
{
    public PointerInfo(
        Point2D modelPoint,
        PointerModifiers modifiers = PointerModifiers.None)
    {
        ModelPoint = modelPoint;
        Modifiers = modifiers;
    }

    public Point2D ModelPoint { get; }

    public PointerModifiers Modifiers { get; }

    public bool IsShiftPressed =>
        Modifiers.HasFlag(PointerModifiers.Shift);

    public bool IsControlPressed =>
        Modifiers.HasFlag(PointerModifiers.Control);

    public bool IsAltPressed =>
        Modifiers.HasFlag(PointerModifiers.Alt);
}