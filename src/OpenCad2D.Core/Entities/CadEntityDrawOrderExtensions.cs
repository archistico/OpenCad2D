using OpenCad2D.Core.Dimensions;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Provides immutable helpers for changing entity draw order without altering geometry.
/// </summary>
public static class CadEntityDrawOrderExtensions
{
    public static CadEntity WithDrawOrder(
        this CadEntity entity,
        int drawOrder)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity switch
        {
            LineEntity line => new LineEntity(
                line.Start,
                line.End,
                line.Id,
                line.LayerId,
                line.Style,
                line.IsVisible,
                line.IsLocked,
                drawOrder),

            CircleEntity circle => new CircleEntity(
                circle.Center,
                circle.Radius,
                circle.Id,
                circle.LayerId,
                circle.Style,
                circle.IsVisible,
                circle.IsLocked,
                drawOrder),

            ArcEntity arc => new ArcEntity(
                arc.Center,
                arc.Radius,
                arc.StartAngle,
                arc.EndAngle,
                arc.IsCounterClockwise,
                arc.Id,
                arc.LayerId,
                arc.Style,
                arc.IsVisible,
                arc.IsLocked,
                drawOrder),

            PolylineEntity polyline => new PolylineEntity(
                polyline.Vertices,
                polyline.IsClosed,
                polyline.Id,
                polyline.LayerId,
                polyline.Style,
                polyline.IsVisible,
                polyline.IsLocked,
                drawOrder),

            BezierSplineEntity spline => new BezierSplineEntity(
                spline.ControlPoints,
                spline.IsClosed,
                spline.Id,
                spline.LayerId,
                spline.Style,
                spline.IsVisible,
                spline.IsLocked,
                drawOrder),

            PointEntity point => new PointEntity(
                point.Position,
                point.Id,
                point.LayerId,
                point.Style,
                point.IsVisible,
                point.IsLocked,
                drawOrder),

            TextEntity text => new TextEntity(
                text.InsertionPoint,
                text.Text,
                text.RotationDegrees,
                text.TextFormatId,
                text.Id,
                text.LayerId,
                text.Style,
                text.IsVisible,
                text.IsLocked,
                drawOrder),

            MultilineTextEntity multilineText => new MultilineTextEntity(
                multilineText.InsertionPoint,
                multilineText.Text,
                multilineText.RotationDegrees,
                multilineText.TextFormatId,
                multilineText.Id,
                multilineText.LayerId,
                multilineText.Style,
                multilineText.IsVisible,
                multilineText.IsLocked,
                drawOrder),

            LinearDimensionEntity linear => new LinearDimensionEntity(
                linear.FirstPoint,
                linear.SecondPoint,
                linear.DimensionLinePoint,
                linear.Orientation,
                linear.DimensionStyleId,
                linear.TextOverride,
                linear.Id,
                linear.LayerId,
                linear.Style,
                linear.IsVisible,
                linear.IsLocked,
                drawOrder),

            AlignedDimensionEntity aligned => new AlignedDimensionEntity(
                aligned.FirstPoint,
                aligned.SecondPoint,
                aligned.DimensionLinePoint,
                aligned.DimensionStyleId,
                aligned.TextOverride,
                aligned.Id,
                aligned.LayerId,
                aligned.Style,
                aligned.IsVisible,
                aligned.IsLocked,
                drawOrder),

            AngularDimensionEntity angular => new AngularDimensionEntity(
                angular.Center,
                angular.FirstRayPoint,
                angular.SecondRayPoint,
                angular.ArcPoint,
                angular.IsCounterClockwise,
                angular.DimensionStyleId,
                angular.TextOverride,
                angular.Id,
                angular.LayerId,
                angular.Style,
                angular.IsVisible,
                angular.IsLocked,
                drawOrder),

            RadiusDimensionEntity radius => new RadiusDimensionEntity(
                radius.Center,
                radius.PointOnCircle,
                radius.TextPoint,
                radius.DimensionStyleId,
                radius.TextOverride,
                radius.Id,
                radius.LayerId,
                radius.Style,
                radius.IsVisible,
                radius.IsLocked,
                drawOrder),

            DiameterDimensionEntity diameter => new DiameterDimensionEntity(
                diameter.Center,
                diameter.PointOnCircle,
                diameter.TextPoint,
                diameter.DimensionStyleId,
                diameter.TextOverride,
                diameter.Id,
                diameter.LayerId,
                diameter.Style,
                diameter.IsVisible,
                diameter.IsLocked,
                drawOrder),

            StairEntity stair => new StairEntity(
                stair.InsertionPoint,
                stair.ViewKind,
                stair.Width,
                stair.TreadCount,
                stair.TreadDepth,
                stair.RiserHeight,
                stair.ShowStructure,
                stair.SlabThickness,
                stair.XAxis,
                stair.YAxis,
                stair.Id,
                stair.LayerId,
                stair.Style,
                stair.IsVisible,
                stair.IsLocked,
                drawOrder,
                stair.PlanArrowMode,
                stair.ShowPlanSectionMarker),

            _ => throw new NotSupportedException(
                $"Entity kind '{entity.Kind}' does not support draw order editing.")
        };
    }
}
