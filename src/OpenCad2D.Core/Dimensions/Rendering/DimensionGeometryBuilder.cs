using System.Globalization;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Dimensions.Rendering;

/// <summary>
/// Builds renderer-agnostic primitives for dimension entities.
/// </summary>
public sealed class DimensionGeometryBuilder
{
    private const double ArrowHalfAngleDegrees = 25.0;
    private const double EstimatedTextWidthFactor = 0.6;
    private const double LinearTextClearanceAdjustment = 12.0;

    public DimensionRenderModel Build(
        DimensionEntity dimension,
        DimensionStyle style)
    {
        ArgumentNullException.ThrowIfNull(dimension);
        ArgumentNullException.ThrowIfNull(style);

        return dimension switch
        {
            LinearDimensionEntity linear => BuildLinear(linear, style),
            AlignedDimensionEntity aligned => BuildAligned(aligned, style),
            RadiusDimensionEntity radius => BuildRadius(radius, style),
            DiameterDimensionEntity diameter => BuildDiameter(diameter, style),
            AngularDimensionEntity angular => BuildAngular(angular, style),
            _ => throw new NotSupportedException(
                $"Dimension type '{dimension.GetType().Name}' is not supported by the geometry builder.")
        };
    }

    public string FormatMeasurement(
        DimensionEntity dimension,
        DimensionStyle style)
    {
        ArgumentNullException.ThrowIfNull(dimension);
        ArgumentNullException.ThrowIfNull(style);

        if (!string.IsNullOrWhiteSpace(dimension.TextOverride))
        {
            return dimension.TextOverride.Trim();
        }

        string text = dimension.MeasurementValue.ToString(
            $"F{style.DecimalPlaces}",
            CultureInfo.InvariantCulture);

        if (style.DecimalSeparator == ",")
        {
            text = text.Replace('.', ',');
        }

        return dimension switch
        {
            RadiusDimensionEntity => "R " + text + style.Suffix,
            DiameterDimensionEntity => "Ø " + text + style.Suffix,
            AngularDimensionEntity => text + "°" + style.Suffix,
            _ => text + style.Suffix
        };
    }

    private DimensionRenderModel BuildLinear(
        LinearDimensionEntity dimension,
        DimensionStyle style)
    {
        Vector2D direction = dimension.Orientation == DimensionOrientation.Horizontal
            ? new Vector2D(1, 0)
            : new Vector2D(0, 1);

        Vector2D normal = dimension.Orientation == DimensionOrientation.Horizontal
            ? new Vector2D(0, 1)
            : new Vector2D(1, 0);

        double offset = dimension.Orientation == DimensionOrientation.Horizontal
            ? dimension.DimensionLinePoint.Y - ((dimension.FirstPoint.Y + dimension.SecondPoint.Y) / 2.0)
            : dimension.DimensionLinePoint.X - ((dimension.FirstPoint.X + dimension.SecondPoint.X) / 2.0);

        int side = offset < 0 ? -1 : 1;
        Vector2D sideNormal = normal * side;

        Point2D firstProjection = dimension.Orientation == DimensionOrientation.Horizontal
            ? new Point2D(dimension.FirstPoint.X, dimension.DimensionLinePoint.Y)
            : new Point2D(dimension.DimensionLinePoint.X, dimension.FirstPoint.Y);

        Point2D secondProjection = dimension.Orientation == DimensionOrientation.Horizontal
            ? new Point2D(dimension.SecondPoint.X, dimension.DimensionLinePoint.Y)
            : new Point2D(dimension.DimensionLinePoint.X, dimension.SecondPoint.Y);

        Vector2D textOffsetDirection = dimension.Orientation == DimensionOrientation.Horizontal
            ? new Vector2D(0, -1)
            : new Vector2D(1, 0);

        return BuildFromProjectedPoints(
            dimension,
            style,
            direction,
            sideNormal,
            dimension.FirstPoint,
            dimension.SecondPoint,
            firstProjection,
            secondProjection,
            textOffsetDirection,
            style.TextOffset + LinearTextClearanceAdjustment,
            dimension.Orientation == DimensionOrientation.Horizontal ? 0.0 : 90.0);
    }

    private DimensionRenderModel BuildAligned(
        AlignedDimensionEntity dimension,
        DimensionStyle style)
    {
        Vector2D direction = (dimension.SecondPoint - dimension.FirstPoint).Normalize();
        Vector2D normal = direction.PerpendicularLeft();
        double offset = (dimension.DimensionLinePoint - dimension.FirstPoint).Dot(normal);
        int side = offset < 0 ? -1 : 1;
        Vector2D offsetVector = normal * offset;
        Vector2D sideNormal = normal * side;

        Point2D firstProjection = dimension.FirstPoint + offsetVector;
        Point2D secondProjection = dimension.SecondPoint + offsetVector;
        double angle = ToDegrees(Math.Atan2(direction.Y, direction.X));

        return BuildFromProjectedPoints(
            dimension,
            style,
            direction,
            sideNormal,
            dimension.FirstPoint,
            dimension.SecondPoint,
            firstProjection,
            secondProjection,
            sideNormal * -1,
            style.TextOffset + LinearTextClearanceAdjustment,
            angle);
    }


    private DimensionRenderModel BuildAngular(
        AngularDimensionEntity dimension,
        DimensionStyle style)
    {
        double radius = dimension.Radius;
        double startAngle = dimension.StartAngleDegrees;
        double endAngle = dimension.EndAngleDegrees;
        double sweep = dimension.MeasurementValue;
        double midAngle = dimension.IsCounterClockwise
            ? NormalizeAngle(startAngle + sweep / 2.0)
            : NormalizeAngle(startAngle - sweep / 2.0);

        Point2D startPoint = PointOnCircle(
            dimension.Center,
            radius,
            startAngle);
        Point2D endPoint = PointOnCircle(
            dimension.Center,
            radius,
            endAngle);

        var lines = new List<DimensionLinePrimitive>
        {
            new(dimension.Center, startPoint),
            new(dimension.Center, endPoint)
        };

        var arcs = new List<DimensionArcPrimitive>
        {
            new(
                dimension.Center,
                radius,
                startAngle,
                endAngle,
                dimension.IsCounterClockwise)
        };

        var arrows = new List<DimensionLinePrimitive>();
        Vector2D startTangent = dimension.IsCounterClockwise
            ? UnitAt(startAngle).PerpendicularLeft()
            : UnitAt(startAngle).PerpendicularRight();
        Vector2D endTangent = dimension.IsCounterClockwise
            ? UnitAt(endAngle).PerpendicularRight()
            : UnitAt(endAngle).PerpendicularLeft();

        AddArrow(
            arrows,
            startPoint,
            startTangent,
            style.ArrowSize);
        AddArrow(
            arrows,
            endPoint,
            endTangent,
            style.ArrowSize);

        Point2D textPosition = PointOnCircle(
            dimension.Center,
            radius + style.TextOffset + LinearTextClearanceAdjustment,
            midAngle);

        var text = new DimensionTextPrimitive(
            FormatMeasurement(dimension, style),
            textPosition,
            NormalizeAngle(midAngle));

        BoundingBox2D bounds = BuildBounds(
            lines,
            arcs,
            arrows,
            text,
            style);

        return new DimensionRenderModel(
            lines,
            arcs,
            arrows,
            text,
            bounds);
    }


    private DimensionRenderModel BuildRadius(
        RadiusDimensionEntity dimension,
        DimensionStyle style)
    {
        Vector2D radiusVector = (dimension.PointOnCircle - dimension.Center).Normalize();
        double angle = ToDegrees(Math.Atan2(radiusVector.Y, radiusVector.X));

        var lines = new List<DimensionLinePrimitive>
        {
            new(dimension.Center, dimension.PointOnCircle),
            new(dimension.PointOnCircle, dimension.TextPoint)
        };

        var arrows = new List<DimensionLinePrimitive>();
        AddArrow(
            arrows,
            dimension.PointOnCircle,
            radiusVector * -1,
            style.ArrowSize);

        var text = new DimensionTextPrimitive(
            FormatMeasurement(dimension, style),
            dimension.TextPoint,
            NormalizeAngle(angle));

        BoundingBox2D bounds = BuildBounds(
            lines,
            arrows,
            text,
            style);

        return new DimensionRenderModel(
            lines,
            arrows,
            text,
            bounds);
    }

    private DimensionRenderModel BuildDiameter(
        DiameterDimensionEntity dimension,
        DimensionStyle style)
    {
        Vector2D radiusVector = (dimension.PointOnCircle - dimension.Center).Normalize();
        double angle = ToDegrees(Math.Atan2(radiusVector.Y, radiusVector.X));
        Point2D oppositePoint = dimension.OppositePoint;

        var lines = new List<DimensionLinePrimitive>
        {
            new(oppositePoint, dimension.PointOnCircle),
            new(dimension.PointOnCircle, dimension.TextPoint)
        };

        var arrows = new List<DimensionLinePrimitive>();
        AddArrow(
            arrows,
            dimension.PointOnCircle,
            radiusVector * -1,
            style.ArrowSize);
        AddArrow(
            arrows,
            oppositePoint,
            radiusVector,
            style.ArrowSize);

        var text = new DimensionTextPrimitive(
            FormatMeasurement(dimension, style),
            dimension.TextPoint,
            NormalizeAngle(angle));

        BoundingBox2D bounds = BuildBounds(
            lines,
            arrows,
            text,
            style);

        return new DimensionRenderModel(
            lines,
            arrows,
            text,
            bounds);
    }

    private DimensionRenderModel BuildFromProjectedPoints(
        DimensionEntity dimension,
        DimensionStyle style,
        Vector2D direction,
        Vector2D sideNormal,
        Point2D firstMeasuredPoint,
        Point2D secondMeasuredPoint,
        Point2D firstProjection,
        Point2D secondProjection,
        Vector2D textOffsetDirection,
        double textOffsetDistance,
        double textRotationDegrees)
    {
        var lines = new List<DimensionLinePrimitive>
        {
            new(firstProjection, secondProjection),
            new(
                firstMeasuredPoint + sideNormal * style.ExtensionLineOffset,
                firstProjection + sideNormal * style.ExtensionLineOvershoot),
            new(
                secondMeasuredPoint + sideNormal * style.ExtensionLineOffset,
                secondProjection + sideNormal * style.ExtensionLineOvershoot)
        };

        var arrows = new List<DimensionLinePrimitive>();
        AddArrow(
            arrows,
            firstProjection,
            direction,
            style.ArrowSize);
        AddArrow(
            arrows,
            secondProjection,
            direction * -1,
            style.ArrowSize);

        Point2D midpoint = new(
            (firstProjection.X + secondProjection.X) / 2.0,
            (firstProjection.Y + secondProjection.Y) / 2.0);
        Point2D textPosition = midpoint + textOffsetDirection.Normalize() * textOffsetDistance;

        var text = new DimensionTextPrimitive(
            FormatMeasurement(dimension, style),
            textPosition,
            NormalizeAngle(textRotationDegrees));

        BoundingBox2D bounds = BuildBounds(
            lines,
            arrows,
            text,
            style);

        return new DimensionRenderModel(
            lines,
            arrows,
            text,
            bounds);
    }

    private static void AddArrow(
        ICollection<DimensionLinePrimitive> arrows,
        Point2D tip,
        Vector2D inwardDirection,
        double arrowSize)
    {
        Vector2D direction = inwardDirection.Normalize();
        Vector2D firstWing = Rotate(direction, 180.0 - ArrowHalfAngleDegrees) * arrowSize;
        Vector2D secondWing = Rotate(direction, 180.0 + ArrowHalfAngleDegrees) * arrowSize;

        arrows.Add(new DimensionLinePrimitive(tip, tip + firstWing));
        arrows.Add(new DimensionLinePrimitive(tip, tip + secondWing));
    }

    private static BoundingBox2D BuildBounds(
        IReadOnlyList<DimensionLinePrimitive> lines,
        IReadOnlyList<DimensionLinePrimitive> arrows,
        DimensionTextPrimitive text,
        DimensionStyle style)
    {
        return BuildBounds(
            lines,
            Array.Empty<DimensionArcPrimitive>(),
            arrows,
            text,
            style);
    }

    private static BoundingBox2D BuildBounds(
        IReadOnlyList<DimensionLinePrimitive> lines,
        IReadOnlyList<DimensionArcPrimitive> arcs,
        IReadOnlyList<DimensionLinePrimitive> arrows,
        DimensionTextPrimitive text,
        DimensionStyle style)
    {
        var points = new List<Point2D>();

        foreach (DimensionLinePrimitive line in lines.Concat(arrows))
        {
            points.Add(line.Start);
            points.Add(line.End);
        }

        foreach (DimensionArcPrimitive arc in arcs)
        {
            BoundingBox2D arcBounds = arc.ToArc2D().GetBoundingBox();
            points.Add(new Point2D(arcBounds.MinX, arcBounds.MinY));
            points.Add(new Point2D(arcBounds.MaxX, arcBounds.MaxY));
        }

        double estimatedTextWidth = Math.Max(
            style.ArrowSize,
            text.Text.Length * style.ArrowSize * EstimatedTextWidthFactor);
        double estimatedTextHeight = Math.Max(
            style.ArrowSize,
            style.ArrowSize);

        points.Add(text.Position);
        points.Add(text.Position + new Vector2D(estimatedTextWidth, estimatedTextHeight));
        points.Add(text.Position - new Vector2D(estimatedTextWidth, estimatedTextHeight));

        double minX = points.Min(point => point.X);
        double minY = points.Min(point => point.Y);
        double maxX = points.Max(point => point.X);
        double maxY = points.Max(point => point.Y);

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    private static Point2D PointOnCircle(
        Point2D center,
        double radius,
        double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;

        return new Point2D(
            center.X + Math.Cos(radians) * radius,
            center.Y + Math.Sin(radians) * radius);
    }

    private static Vector2D UnitAt(double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;

        return new Vector2D(
            Math.Cos(radians),
            Math.Sin(radians));
    }

    private static Vector2D Rotate(
        Vector2D vector,
        double degrees)
    {
        double radians = degrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        return new Vector2D(
            (vector.X * cos) - (vector.Y * sin),
            (vector.X * sin) + (vector.Y * cos));
    }

    private static double ToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    private static double NormalizeAngle(double degrees)
    {
        double normalized = degrees % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }
}
