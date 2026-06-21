using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Anchors;

/// <summary>
/// Shared helper for resolving canonical 9-point anchors against CAD-oriented
/// bounding boxes. CAD top is <see cref="BoundingBox2D.MaxY"/> and bottom is
/// <see cref="BoundingBox2D.MinY"/>; screen coordinate inversion must be handled
/// only by rendering/UI layers.
/// </summary>
public static class AnchorPointService
{
    private static readonly AnchorPointDescriptor[] DescriptorValues =
    {
        new(AnchorPoint.TopLeft, "TopLeft", "Top left", 0, 0, 7),
        new(AnchorPoint.TopCenter, "TopCenter", "Top center", 0, 1, 8),
        new(AnchorPoint.TopRight, "TopRight", "Top right", 0, 2, 9),
        new(AnchorPoint.MiddleLeft, "MiddleLeft", "Middle left", 1, 0, 4),
        new(AnchorPoint.Center, "Center", "Center", 1, 1, 5),
        new(AnchorPoint.MiddleRight, "MiddleRight", "Middle right", 1, 2, 6),
        new(AnchorPoint.BottomLeft, "BottomLeft", "Bottom left", 2, 0, 1),
        new(AnchorPoint.BottomCenter, "BottomCenter", "Bottom center", 2, 1, 2),
        new(AnchorPoint.BottomRight, "BottomRight", "Bottom right", 2, 2, 3)
    };

    /// <summary>
    /// Gets all anchors in visual 3x3 selector order, from top-left to bottom-right.
    /// </summary>
    public static IReadOnlyList<AnchorPointDescriptor> Descriptors => DescriptorValues;

    public static AnchorPointDescriptor GetDescriptor(AnchorPoint anchor)
    {
        foreach (AnchorPointDescriptor descriptor in DescriptorValues)
        {
            if (descriptor.Anchor == anchor)
            {
                return descriptor;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "Unknown anchor point.");
    }

    public static AnchorPoint FromGridPosition(int row, int column)
    {
        foreach (AnchorPointDescriptor descriptor in DescriptorValues)
        {
            if (descriptor.Row == row && descriptor.Column == column)
            {
                return descriptor.Anchor;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(row),
            $"Anchor grid position must be inside the 3x3 range. Received row {row}, column {column}.");
    }

    public static bool TryFromNumericShortcut(int shortcut, out AnchorPoint anchor)
    {
        foreach (AnchorPointDescriptor descriptor in DescriptorValues)
        {
            if (descriptor.NumericShortcut == shortcut)
            {
                anchor = descriptor.Anchor;
                return true;
            }
        }

        anchor = AnchorPoint.Center;
        return false;
    }

    public static bool TryParse(string? value, out AnchorPoint anchor)
    {
        if (Enum.TryParse(value, ignoreCase: false, out anchor))
        {
            return true;
        }

        anchor = AnchorPoint.Center;
        return false;
    }

    public static AnchorPoint ParseOrDefault(
        string? value,
        AnchorPoint defaultAnchor = AnchorPoint.Center)
    {
        return TryParse(value, out AnchorPoint anchor)
            ? anchor
            : defaultAnchor;
    }

    public static Point2D GetPoint(BoundingBox2D bounds, AnchorPoint anchor)
    {
        double centerX = (bounds.MinX + bounds.MaxX) / 2.0;
        double centerY = (bounds.MinY + bounds.MaxY) / 2.0;

        return anchor switch
        {
            AnchorPoint.TopLeft => new Point2D(bounds.MinX, bounds.MaxY),
            AnchorPoint.TopCenter => new Point2D(centerX, bounds.MaxY),
            AnchorPoint.TopRight => new Point2D(bounds.MaxX, bounds.MaxY),
            AnchorPoint.MiddleLeft => new Point2D(bounds.MinX, centerY),
            AnchorPoint.Center => new Point2D(centerX, centerY),
            AnchorPoint.MiddleRight => new Point2D(bounds.MaxX, centerY),
            AnchorPoint.BottomLeft => new Point2D(bounds.MinX, bounds.MinY),
            AnchorPoint.BottomCenter => new Point2D(centerX, bounds.MinY),
            AnchorPoint.BottomRight => new Point2D(bounds.MaxX, bounds.MinY),
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "Unknown anchor point.")
        };
    }

    public static Vector2D GetTranslationToPlaceAnchor(
        BoundingBox2D localBounds,
        AnchorPoint anchor,
        Point2D targetPoint)
    {
        Point2D localAnchorPoint = GetPoint(localBounds, anchor);
        return targetPoint - localAnchorPoint;
    }

    public static AnchorPlacement CreatePlacement(
        BoundingBox2D localBounds,
        AnchorPoint anchor,
        Point2D targetPoint)
    {
        Point2D localAnchorPoint = GetPoint(localBounds, anchor);
        Vector2D translation = targetPoint - localAnchorPoint;

        return new AnchorPlacement(
            anchor,
            localAnchorPoint,
            targetPoint,
            translation);
    }
}
