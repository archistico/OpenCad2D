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
    private const double LinearTextOutsideGapFactor = 1.5;

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
            RadiusDimensionEntity => style.RadiusPrefix + text + style.Suffix,
            DiameterDimensionEntity => style.DiameterPrefix + text + style.Suffix,
            AngularDimensionEntity => style.Prefix + text + "°" + style.Suffix,
            _ => style.Prefix + text + style.Suffix
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
            : new Vector2D(-1, 0);

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
            ResolveTextRotationAngle(
                dimension.Orientation == DimensionOrientation.Horizontal ? 0.0 : 90.0,
                style));
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
            ResolveTextRotationAngle(angle, style));
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

        AddTerminator(
            arrows,
            startPoint,
            startTangent,
            style.ArrowSize,
            style.ArrowSymbol);
        AddTerminator(
            arrows,
            endPoint,
            endTangent,
            style.ArrowSize,
            style.ArrowSymbol);

        Point2D textPosition = PointOnCircle(
            dimension.Center,
            radius + style.TextOffset + LinearTextClearanceAdjustment,
            midAngle);

        var text = new DimensionTextPrimitive(
            FormatMeasurement(dimension, style),
            textPosition,
            ResolveTextRotationAngle(midAngle, style));

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
        AddTerminator(
            arrows,
            dimension.PointOnCircle,
            radiusVector * -1,
            style.ArrowSize,
            style.ArrowSymbol);

        var text = new DimensionTextPrimitive(
            FormatMeasurement(dimension, style),
            dimension.TextPoint,
            ResolveTextRotationAngle(angle, style));

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
        AddTerminator(
            arrows,
            dimension.PointOnCircle,
            radiusVector * -1,
            style.ArrowSize,
            style.ArrowSymbol);
        AddTerminator(
            arrows,
            oppositePoint,
            radiusVector,
            style.ArrowSize,
            style.ArrowSymbol);

        var text = new DimensionTextPrimitive(
            FormatMeasurement(dimension, style),
            dimension.TextPoint,
            ResolveTextRotationAngle(angle, style));

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

        string measurementText = FormatMeasurement(dimension, style);
        bool placeTerminatorsOutside = ShouldPlaceTerminatorsOutside(
            firstProjection,
            secondProjection,
            measurementText,
            style);

        var arrows = new List<DimensionLinePrimitive>();
        AddTerminator(
            arrows,
            firstProjection,
            placeTerminatorsOutside ? direction : direction * -1,
            style.ArrowSize,
            style.ArrowSymbol);
        AddTerminator(
            arrows,
            secondProjection,
            placeTerminatorsOutside ? direction * -1 : direction,
            style.ArrowSize,
            style.ArrowSymbol);

        Point2D midpoint = new(
            (firstProjection.X + secondProjection.X) / 2.0,
            (firstProjection.Y + secondProjection.Y) / 2.0);
        Point2D textPosition = ResolveLinearTextPosition(
            midpoint,
            firstProjection,
            secondProjection,
            direction,
            textOffsetDirection,
            textOffsetDistance,
            measurementText,
            style);

        var text = new DimensionTextPrimitive(
            measurementText,
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



    private static bool ShouldPlaceTerminatorsOutside(
        Point2D firstProjection,
        Point2D secondProjection,
        string measurementText,
        DimensionStyle style)
    {
        return style.TerminatorFitMode switch
        {
            DimensionTerminatorFitMode.Inside => false,
            DimensionTerminatorFitMode.AlwaysOutside => true,
            _ => firstProjection.DistanceTo(secondProjection) < EstimateTerminatorInsideSpan(measurementText, style)
        };
    }

    private static double EstimateTerminatorInsideSpan(
        string measurementText,
        DimensionStyle style)
    {
        double textSpan = style.TextFitMode == DimensionTextFitMode.Inside
            ? EstimateTextSpan(measurementText, style)
            : 0.0;

        return Math.Max(
            style.ArrowSize * 3.0,
            (style.ArrowSize * 2.0) + textSpan);
    }

    private static Point2D ResolveLinearTextPosition(
        Point2D midpoint,
        Point2D firstProjection,
        Point2D secondProjection,
        Vector2D direction,
        Vector2D textOffsetDirection,
        double textOffsetDistance,
        string measurementText,
        DimensionStyle style)
    {
        Vector2D normalizedTextOffsetDirection = textOffsetDirection.Normalize();
        Point2D insidePosition = midpoint + normalizedTextOffsetDirection * textOffsetDistance;

        if (style.TextFitMode == DimensionTextFitMode.Inside)
        {
            return insidePosition;
        }

        double measuredSpan = firstProjection.DistanceTo(secondProjection);
        double requiredTextSpan = EstimateTextSpan(measurementText, style);

        if (style.TextFitMode == DimensionTextFitMode.OutsideWhenNeeded &&
            measuredSpan >= requiredTextSpan)
        {
            return insidePosition;
        }

        Vector2D normalizedDirection = direction.Normalize();
        double outsideDistance = (measuredSpan / 2.0) +
                                 (requiredTextSpan / 2.0) +
                                 (style.ArrowSize * LinearTextOutsideGapFactor);
        return midpoint + normalizedDirection * outsideDistance + normalizedTextOffsetDirection * textOffsetDistance;
    }

    private static double EstimateTextSpan(
        string text,
        DimensionStyle style)
    {
        return Math.Max(
            style.ArrowSize * 2.0,
            text.Length * style.ArrowSize * EstimatedTextWidthFactor);
    }

    private static void AddTerminator(
        ICollection<DimensionLinePrimitive> arrows,
        Point2D tip,
        Vector2D inwardDirection,
        double arrowSize,
        DimensionArrowSymbol arrowSymbol)
    {
        if (arrowSymbol == DimensionArrowSymbol.None)
        {
            return;
        }

        Vector2D direction = inwardDirection.Normalize();

        switch (arrowSymbol)
        {
            case DimensionArrowSymbol.ArchitecturalTick:
                AddTickTerminator(
                    arrows,
                    tip,
                    direction,
                    arrowSize,
                    135.0,
                    0.75);
                return;

            case DimensionArrowSymbol.ObliqueSlash:
                AddTickTerminator(
                    arrows,
                    tip,
                    direction,
                    arrowSize,
                    135.0,
                    1.0);
                return;

            case DimensionArrowSymbol.Dot:
                AddDotTerminator(
                    arrows,
                    tip,
                    arrowSize);
                return;

            case DimensionArrowSymbol.OpenArrow:
                AddOpenArrowTerminator(
                    arrows,
                    tip,
                    direction,
                    arrowSize);
                return;

            case DimensionArrowSymbol.ClosedFilledTriangle:
                AddTriangleTerminator(
                    arrows,
                    tip,
                    direction,
                    arrowSize,
                    addBase: true,
                    addFillStrokes: true);
                return;

            case DimensionArrowSymbol.ClosedBlankTriangle:
            case DimensionArrowSymbol.ClosedArrow:
                AddTriangleTerminator(
                    arrows,
                    tip,
                    direction,
                    arrowSize,
                    addBase: true,
                    addFillStrokes: false);
                return;

            case DimensionArrowSymbol.FilledTriangleOutside:
                AddTriangleTerminator(
                    arrows,
                    tip,
                    direction * -1,
                    arrowSize,
                    addBase: true,
                    addFillStrokes: true);
                return;
        }
    }

    private static void AddTickTerminator(
        ICollection<DimensionLinePrimitive> arrows,
        Point2D tip,
        Vector2D direction,
        double arrowSize,
        double angleDegrees,
        double lengthFactor)
    {
        Vector2D tick = Rotate(direction, angleDegrees).Normalize() * (arrowSize * lengthFactor);
        arrows.Add(new DimensionLinePrimitive(tip - tick, tip + tick));
    }

    private static void AddDotTerminator(
        ICollection<DimensionLinePrimitive> arrows,
        Point2D tip,
        double arrowSize)
    {
        double radius = arrowSize * 0.25;
        Vector2D horizontal = new(radius, 0);
        Vector2D vertical = new(0, radius);
        arrows.Add(new DimensionLinePrimitive(tip - horizontal, tip + horizontal));
        arrows.Add(new DimensionLinePrimitive(tip - vertical, tip + vertical));
    }

    private static void AddOpenArrowTerminator(
        ICollection<DimensionLinePrimitive> arrows,
        Point2D tip,
        Vector2D direction,
        double arrowSize)
    {
        AddTriangleTerminator(
            arrows,
            tip,
            direction,
            arrowSize,
            addBase: false,
            addFillStrokes: false);
    }

    private static void AddTriangleTerminator(
        ICollection<DimensionLinePrimitive> arrows,
        Point2D tip,
        Vector2D direction,
        double arrowSize,
        bool addBase,
        bool addFillStrokes)
    {
        Vector2D firstWing = Rotate(direction, 180.0 - ArrowHalfAngleDegrees) * arrowSize;
        Vector2D secondWing = Rotate(direction, 180.0 + ArrowHalfAngleDegrees) * arrowSize;

        Point2D firstEnd = tip + firstWing;
        Point2D secondEnd = tip + secondWing;
        arrows.Add(new DimensionLinePrimitive(tip, firstEnd));
        arrows.Add(new DimensionLinePrimitive(tip, secondEnd));

        if (addBase)
        {
            arrows.Add(new DimensionLinePrimitive(firstEnd, secondEnd));
        }

        if (addFillStrokes)
        {
            Point2D baseMidpoint = new(
                (firstEnd.X + secondEnd.X) / 2.0,
                (firstEnd.Y + secondEnd.Y) / 2.0);
            Point2D firstFillPoint = Lerp(tip, firstEnd, 0.55);
            Point2D secondFillPoint = Lerp(tip, secondEnd, 0.55);

            arrows.Add(new DimensionLinePrimitive(tip, baseMidpoint));
            arrows.Add(new DimensionLinePrimitive(firstFillPoint, secondFillPoint));
        }
    }

    private static Point2D Lerp(
        Point2D first,
        Point2D second,
        double factor)
    {
        return new Point2D(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);
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
        double estimatedTextHeight = style.ArrowSize;

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



    private static double ResolveTextRotationAngle(
        double rawDegrees,
        DimensionStyle style)
    {
        return style.TextRotationMode switch
        {
            DimensionTextRotationMode.Horizontal => 0.0,
            DimensionTextRotationMode.AlignedWithDimensionLine => NormalizeAngle(rawDegrees),
            _ => NormalizeReadableAngle(rawDegrees)
        };
    }

    private static double NormalizeReadableAngle(double degrees)
    {
        double normalized = NormalizeAngle(degrees);

        if (normalized > 90.0 && normalized <= 270.0)
        {
            normalized = NormalizeAngle(normalized + 180.0);
        }

        return SnapReadableVerticalAngle(normalized);
    }

    private static double SnapReadableVerticalAngle(double degrees)
    {
        const double epsilon = 1e-9;

        if (Math.Abs(degrees) <= epsilon || Math.Abs(degrees - 360.0) <= epsilon)
        {
            return 0.0;
        }

        if (Math.Abs(degrees - 90.0) <= epsilon || Math.Abs(degrees - 270.0) <= epsilon)
        {
            return 270.0;
        }

        return degrees;
    }

    private static double NormalizeAngle(double degrees)
    {
        double normalized = degrees % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }
}
