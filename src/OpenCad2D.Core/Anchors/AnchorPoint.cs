namespace OpenCad2D.Core.Anchors;

/// <summary>
/// Canonical 9-point anchor used by insertable and parametric entities.
/// The names are persisted as stable strings; do not rely on ordinal values.
/// </summary>
public enum AnchorPoint
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}
