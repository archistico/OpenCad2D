using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing multiline annotation text.
/// </summary>
public sealed class MultilineTextEntity : CadEntity
{
    private const double DefaultEstimatedHeight = 10.0;
    private const double EstimatedWidthFactor = 0.6;
    private const double LineHeightFactor = 1.2;

    public MultilineTextEntity(
        Point2D insertionPoint,
        string text,
        double rotationDegrees = 0.0,
        TextFormatId? textFormatId = null,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Multiline text cannot be empty.",
                nameof(text));
        }

        TextFormatId resolvedFormatId = textFormatId ?? TextFormatId.Standard;

        if (string.IsNullOrWhiteSpace(resolvedFormatId.Value))
        {
            throw new ArgumentException(
                "Text format id cannot be empty.",
                nameof(textFormatId));
        }

        InsertionPoint = insertionPoint;
        Text = NormalizeText(text);
        RotationDegrees = NormalizeRotation(rotationDegrees);
        TextFormatId = resolvedFormatId;
    }

    public Point2D InsertionPoint { get; }

    public string Text { get; }

    public double RotationDegrees { get; }

    public TextFormatId TextFormatId { get; }

    public IReadOnlyList<string> Lines => Text.Split('\n');

    public override EntityKind Kind => EntityKind.MultilineText;

    public override BoundingBox2D GetBoundingBox()
    {
        return GetEstimatedBoundingBox(DefaultEstimatedHeight);
    }

    public BoundingBox2D GetEstimatedBoundingBox(double height)
    {
        if (height <= 0)
        {
            height = DefaultEstimatedHeight;
        }

        IReadOnlyList<string> lines = Lines;
        double width = Math.Max(
            height,
            lines.Max(line => Math.Max(1, line.Length)) * height * EstimatedWidthFactor);
        double totalHeight = Math.Max(height, lines.Count * height * LineHeightFactor);

        double radians = RotationDegrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        Point2D[] corners =
        {
            InsertionPoint,
            RotateLocal(width, 0, cos, sin),
            RotateLocal(width, totalHeight, cos, sin),
            RotateLocal(0, totalHeight, cos, sin)
        };

        return new BoundingBox2D(
            corners.Min(point => point.X),
            corners.Min(point => point.Y),
            corners.Max(point => point.X),
            corners.Max(point => point.Y));
    }

    public override double DistanceTo(Point2D point)
    {
        BoundingBox2D bounds = GetBoundingBox();

        double dx = Math.Max(
            Math.Max(bounds.MinX - point.X, 0),
            point.X - bounds.MaxX);

        double dy = Math.Max(
            Math.Max(bounds.MinY - point.Y, 0),
            point.Y - bounds.MaxY);

        return Math.Sqrt(dx * dx + dy * dy);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return InsertionPoint;
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        Vector2D transformedXAxis = matrix.Transform(new Vector2D(1, 0));
        double rotationDeltaDegrees = Math.Atan2(transformedXAxis.Y, transformedXAxis.X) * 180.0 / Math.PI;

        return new MultilineTextEntity(
            matrix.Transform(InsertionPoint),
            Text,
            RotationDegrees + rotationDeltaDegrees,
            TextFormatId,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new MultilineTextEntity(
            InsertionPoint,
            Text,
            RotationDegrees,
            TextFormatId,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new MultilineTextEntity(
            InsertionPoint,
            Text,
            RotationDegrees,
            TextFormatId,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public MultilineTextEntity WithText(string text)
    {
        return new MultilineTextEntity(
            InsertionPoint,
            text,
            RotationDegrees,
            TextFormatId,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public MultilineTextEntity WithInsertionPoint(Point2D insertionPoint)
    {
        return new MultilineTextEntity(
            insertionPoint,
            Text,
            RotationDegrees,
            TextFormatId,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public MultilineTextEntity WithTextFormat(TextFormatId textFormatId)
    {
        return new MultilineTextEntity(
            InsertionPoint,
            Text,
            RotationDegrees,
            textFormatId,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    private Point2D RotateLocal(
        double localX,
        double localY,
        double cos,
        double sin)
    {
        return new Point2D(
            InsertionPoint.X + localX * cos - localY * sin,
            InsertionPoint.Y + localX * sin + localY * cos);
    }

    private static string NormalizeText(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    private static double NormalizeRotation(double rotationDegrees)
    {
        double normalized = rotationDegrees % 360.0;

        return normalized < 0
            ? normalized + 360.0
            : normalized;
    }
}
