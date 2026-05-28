using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Measurements;

/// <summary>
/// Provides pure measurement calculations for points and CAD entities.
/// </summary>
public static class MeasurementService
{
    public static DistanceMeasurement MeasureDistance(
        Point2D firstPoint,
        Point2D secondPoint)
    {
        return new DistanceMeasurement(firstPoint, secondPoint);
    }

    public static AngleMeasurement MeasureAngle(
        Point2D firstRayPoint,
        Point2D vertex,
        Point2D secondRayPoint)
    {
        return new AngleMeasurement(firstRayPoint, vertex, secondRayPoint);
    }

    public static EntityMeasurement MeasureEntity(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity switch
        {
            PointEntity point => MeasurePoint(point),
            TextEntity text => MeasureText(text),
            MultilineTextEntity multilineText => MeasureMultilineText(multilineText),
            LinearDimensionEntity linearDimension => MeasureLinearDimension(linearDimension),
            AlignedDimensionEntity alignedDimension => MeasureAlignedDimension(alignedDimension),
            RadiusDimensionEntity radiusDimension => MeasureRadiusDimension(radiusDimension),
            DiameterDimensionEntity diameterDimension => MeasureDiameterDimension(diameterDimension),
            AngularDimensionEntity angularDimension => MeasureAngularDimension(angularDimension),
            LineEntity line => MeasureLine(line),
            CircleEntity circle => MeasureCircle(circle),
            ArcEntity arc => MeasureArc(arc),
            PolylineEntity polyline => MeasurePolyline(polyline),
            BezierSplineEntity spline => MeasureBezierSpline(spline),
            ImageReferenceEntity imageReference => MeasureImageReference(imageReference),
            _ => throw new NotSupportedException(
                $"Measurements are not supported for entity kind '{entity.Kind}'."),
        };
    }

    public static double CalculatePolylineLength(PolylineEntity polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        return polyline.GetInteractionGeometry().Length;
    }

    public static double? CalculatePolylineArea(PolylineEntity polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        if (!polyline.IsClosed)
        {
            return null;
        }

        return CalculatePolygonArea(polyline.GetInteractionGeometry().Vertices);
    }

    public static double CalculatePolygonArea(IReadOnlyList<Point2D> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        if (vertices.Count < 3)
        {
            return 0.0;
        }

        double signedArea = 0.0;

        for (int index = 0; index < vertices.Count; index++)
        {
            Point2D current = vertices[index];
            Point2D next = vertices[(index + 1) % vertices.Count];

            signedArea += current.X * next.Y - next.X * current.Y;
        }

        return Math.Abs(signedArea) / 2.0;
    }

    public static double CalculateArcSweepRadians(ArcEntity arc)
    {
        ArgumentNullException.ThrowIfNull(arc);

        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;

        if (arc.IsCounterClockwise)
        {
            if (end < start)
            {
                end += 2.0 * Math.PI;
            }

            return end - start;
        }

        if (start < end)
        {
            start += 2.0 * Math.PI;
        }

        return start - end;
    }

    private static EntityMeasurement MeasurePoint(PointEntity point)
    {
        ArgumentNullException.ThrowIfNull(point);

        return new EntityMeasurement(EntityKind.Point);
    }

    private static EntityMeasurement MeasureText(TextEntity text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new EntityMeasurement(EntityKind.Text);
    }

    private static EntityMeasurement MeasureMultilineText(MultilineTextEntity text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new EntityMeasurement(EntityKind.MultilineText);
    }

    private static EntityMeasurement MeasureLinearDimension(LinearDimensionEntity dimension)
    {
        ArgumentNullException.ThrowIfNull(dimension);

        return new EntityMeasurement(
            dimension.Kind,
            length: dimension.MeasurementValue);
    }

    private static EntityMeasurement MeasureAlignedDimension(AlignedDimensionEntity dimension)
    {
        ArgumentNullException.ThrowIfNull(dimension);

        return new EntityMeasurement(
            EntityKind.AlignedDimension,
            length: dimension.MeasurementValue);
    }

    private static EntityMeasurement MeasureRadiusDimension(RadiusDimensionEntity dimension)
    {
        ArgumentNullException.ThrowIfNull(dimension);

        return new EntityMeasurement(
            EntityKind.RadiusDimension,
            radius: dimension.MeasurementValue);
    }

    private static EntityMeasurement MeasureDiameterDimension(DiameterDimensionEntity dimension)
    {
        ArgumentNullException.ThrowIfNull(dimension);

        return new EntityMeasurement(
            EntityKind.DiameterDimension,
            diameter: dimension.MeasurementValue);
    }

    private static EntityMeasurement MeasureAngularDimension(AngularDimensionEntity dimension)
    {
        ArgumentNullException.ThrowIfNull(dimension);

        return new EntityMeasurement(
            EntityKind.AngularDimension,
            sweepAngleDegrees: dimension.MeasurementValue);
    }

    private static EntityMeasurement MeasureLine(LineEntity line)
    {
        DistanceMeasurement measurement = MeasureDistance(line.Start, line.End);

        return new EntityMeasurement(
            EntityKind.Line,
            length: measurement.Distance,
            angleDegrees: measurement.AngleDegrees);
    }

    private static EntityMeasurement MeasureCircle(CircleEntity circle)
    {
        double circumference = 2.0 * Math.PI * circle.Radius;
        double area = Math.PI * circle.Radius * circle.Radius;

        return new EntityMeasurement(
            EntityKind.Circle,
            radius: circle.Radius,
            diameter: circle.Radius * 2.0,
            circumference: circumference,
            area: area);
    }

    private static EntityMeasurement MeasureArc(ArcEntity arc)
    {
        double sweepRadians = CalculateArcSweepRadians(arc);
        double sweepDegrees = sweepRadians * 180.0 / Math.PI;
        double length = arc.Radius * sweepRadians;

        return new EntityMeasurement(
            EntityKind.Arc,
            length: length,
            radius: arc.Radius,
            diameter: arc.Radius * 2.0,
            sweepAngleDegrees: sweepDegrees);
    }

    private static EntityMeasurement MeasurePolyline(PolylineEntity polyline)
    {
        double length = CalculatePolylineLength(polyline);
        double? area = CalculatePolylineArea(polyline);

        return new EntityMeasurement(
            EntityKind.Polyline,
            length: length,
            area: area,
            vertexCount: polyline.Vertices.Count,
            isClosed: polyline.IsClosed);
    }

    private static EntityMeasurement MeasureBezierSpline(BezierSplineEntity spline)
    {
        PolylineEntity approximation = spline.ToPolylineApproximation();

        return new EntityMeasurement(
            EntityKind.BezierSpline,
            length: CalculatePolylineLength(approximation),
            vertexCount: spline.ControlPoints.Count,
            isClosed: spline.IsClosed);
    }

    private static EntityMeasurement MeasureImageReference(ImageReferenceEntity imageReference)
    {
        double area = Math.Abs(imageReference.WidthVector.Cross(imageReference.HeightVector));

        return new EntityMeasurement(
            EntityKind.ImageReference,
            length: imageReference.Width,
            area: area,
            vertexCount: 4,
            isClosed: true);
    }
}
