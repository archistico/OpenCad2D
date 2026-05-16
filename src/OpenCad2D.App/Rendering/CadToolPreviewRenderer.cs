using Avalonia;
using Avalonia.Media;
using OpenCad2D.App.Viewport;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Dimensions;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Grips;
using OpenCad2D.Tools.Measurements;
using OpenCad2D.Tools.Navigation;
using OpenCad2D.Tools.Selection;
using System;
using System.Collections.Generic;

namespace OpenCad2D.App.Rendering;

/// <summary>
/// Renders transient previews for the active CAD tool.
/// </summary>
/// <remarks>
/// The class keeps tool-specific preview drawing out of <c>CadCanvas</c>.
/// It still contains the existing concrete-tool dispatch intentionally; a later
/// step can replace that dispatch with tool-provided preview descriptors.
/// </remarks>
public sealed class CadToolPreviewRenderer
{
    private readonly ViewportTransform _viewport;
    private readonly CadEntityRenderer _entityRenderer;
    private CadWorkspace? _workspace;

    private readonly Pen _previewPen = new(Brushes.Orange, 1);
    private readonly Pen _modifyPreviewHighlightPen = new(
        new SolidColorBrush(Color.FromRgb(255, 90, 90)),
        2.5);
    private readonly Pen _measurementVectorPen = new(
        new SolidColorBrush(Color.FromRgb(255, 190, 70)),
        1.5);
    private readonly Pen _basePointMarkerPen = new(
        new SolidColorBrush(Color.FromRgb(255, 220, 120)),
        1.5);
    private readonly IBrush _basePointMarkerFill = new SolidColorBrush(
        Color.FromArgb(80, 255, 190, 70));
    private readonly Pen _selectionWindowPen = new(Brushes.LightGreen, 1);
    private readonly Pen _zoomWindowPen = new(Brushes.LightSkyBlue, 1.2);
    private readonly IBrush _selectionWindowFill = new SolidColorBrush(Color.FromArgb(35, 80, 180, 255));
    private readonly IBrush _zoomWindowFill = new SolidColorBrush(Color.FromArgb(30, 80, 210, 255));
    private readonly Pen _gripColdPen = new(
        new SolidColorBrush(Color.FromRgb(80, 210, 255)),
        1.5);
    private readonly IBrush _gripColdFill = new SolidColorBrush(
        Color.FromArgb(35, 80, 210, 255));
    private readonly IBrush _gripHotFill = new SolidColorBrush(Color.FromRgb(70, 210, 120));
    private readonly Pen _gripHotPen = new(
        new SolidColorBrush(Color.FromRgb(150, 255, 190)),
        2);
    private readonly Pen _gripHotHaloPen = new(
        new SolidColorBrush(Color.FromArgb(150, 70, 210, 120)),
        1.5);
    private readonly IBrush _gripWarmFill = new SolidColorBrush(Color.FromRgb(255, 90, 90));
    private readonly Pen _gripWarmPen = new(
        new SolidColorBrush(Color.FromRgb(255, 210, 210)),
        2);
    private readonly Pen _gripWarmHaloPen = new(
        new SolidColorBrush(Color.FromArgb(180, 255, 90, 90)),
        1.5);

    public CadToolPreviewRenderer(
        ViewportTransform viewport,
        CadEntityRenderer entityRenderer)
    {
        _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
        _entityRenderer = entityRenderer ?? throw new ArgumentNullException(nameof(entityRenderer));
    }

    public void DrawActiveToolPreview(
        DrawingContext context,
        CadWorkspace? workspace)
    {
        ArgumentNullException.ThrowIfNull(context);

        _workspace = workspace;

        if (_workspace is null)
        {
            return;
        }

        switch (_workspace.ToolController.ActiveTool)
        {
            case LineTool lineTool:
                DrawLinePreview(context, lineTool);
                break;

            case RectangleTool rectangleTool:
                DrawRectanglePreview(context, rectangleTool);
                break;

            case RectangleBySidesTool rectangleBySidesTool:
                DrawRectangleBySidesPreview(context, rectangleBySidesTool);
                break;

            case CircleTool circleTool:
                DrawCirclePreview(context, circleTool);
                break;

            case EllipseTool ellipseTool:
                DrawEllipsePreview(context, ellipseTool);
                break;

            case ArcTool arcTool:
                DrawArcPreview(context, arcTool);
                break;

            case ArcThreePointsTool arcThreePointsTool:
                DrawArcThreePointsPreview(context, arcThreePointsTool);
                break;

            case PolylineTool polylineTool:
                DrawPolylinePreview(context, polylineTool);
                break;

            case SplineTool splineTool:
                DrawSplinePreview(context, splineTool);
                break;

            case PolygonTool polygonTool:
                DrawPolygonPreview(context, polygonTool);
                break;

            case HorizontalDimensionTool horizontalDimensionTool:
                DrawEntitiesPreview(
                    context,
                    horizontalDimensionTool.GetPreviewEntities());
                break;

            case VerticalDimensionTool verticalDimensionTool:
                DrawEntitiesPreview(
                    context,
                    verticalDimensionTool.GetPreviewEntities());
                break;

            case AlignedDimensionTool alignedDimensionTool:
                DrawEntitiesPreview(
                    context,
                    alignedDimensionTool.GetPreviewEntities());
                break;

            case RadiusDimensionTool radiusDimensionTool:
                DrawEntitiesPreview(
                    context,
                    radiusDimensionTool.GetPreviewEntities());
                break;

            case DiameterDimensionTool diameterDimensionTool:
                DrawEntitiesPreview(
                    context,
                    diameterDimensionTool.GetPreviewEntities());
                break;

            case AngularDimensionTool angularDimensionTool:
                DrawEntitiesPreview(
                    context,
                    angularDimensionTool.GetPreviewEntities());
                break;

            case MoveTool moveTool:
                DrawEntitiesPreview(
                    context,
                    moveTool.GetPreviewEntities(_workspace.Context));
                break;

            case CopyTool copyTool:
                DrawEntitiesPreview(
                    context,
                    copyTool.GetPreviewEntities(_workspace.Context));
                break;

            case RotateTool rotateTool:
                DrawEntitiesPreview(
                    context,
                    rotateTool.GetPreviewEntities(_workspace.Context));
                break;

            case ScaleTool scaleTool:
                DrawEntitiesPreview(
                    context,
                    scaleTool.GetPreviewEntities(_workspace.Context));
                break;

            case AlignTool alignTool:
                DrawEntitiesPreview(
                    context,
                    alignTool.GetPreviewEntities(_workspace.Context));
                break;

            case BreakAtPointTool breakAtPointTool:
                DrawEntitiesPreview(
                    context,
                    breakAtPointTool.GetPreviewEntities());
                break;

            case BreakBetweenPointsTool breakBetweenPointsTool:
                DrawEntitiesPreview(
                    context,
                    breakBetweenPointsTool.GetPreviewEntities());
                break;

            case ExtendTool extendTool:
                DrawEntitiesPreview(
                    context,
                    extendTool.GetPreviewEntities());
                DrawEntitiesPreview(
                    context,
                    extendTool.GetHighlightedPreviewEntities(),
                    _modifyPreviewHighlightPen);
                break;

            case TrimTool trimTool:
                DrawEntitiesPreview(
                    context,
                    trimTool.GetPreviewEntities());
                DrawEntitiesPreview(
                    context,
                    trimTool.GetHighlightedPreviewEntities(),
                    _modifyPreviewHighlightPen);
                break;

            case OffsetTool offsetTool:
                DrawOffsetPreview(context, offsetTool);
                break;

            case MirrorTool mirrorTool:
                DrawMirrorPreview(context, mirrorTool);
                break;

            case MeasureDistanceTool measureDistanceTool:
                DrawMeasureDistancePreview(context, measureDistanceTool);
                break;

            case MeasureAngleTool measureAngleTool:
                DrawMeasureAnglePreview(context, measureAngleTool);
                break;

            case SelectionTool selectionTool:
                DrawSelectionPreview(context, selectionTool);
                break;

            case ZoomWindowTool zoomWindowTool:
                DrawZoomWindowPreview(context, zoomWindowTool);
                break;

            case GripEditTool gripEditTool:
                DrawGripEditPreview(context, gripEditTool);
                break;
        }

        if (_workspace.ToolController.ActiveTool is TwoPointToolBase twoPointTool)
        {
            DrawTwoPointToolMeasurementPreview(
                context,
                twoPointTool);
        }
        else if (_workspace.ToolController.ActiveTool is MoveTool moveTool)
        {
            DrawMoveToolMeasurementPreview(
                context,
                moveTool);
        }
        else if (_workspace.ToolController.ActiveTool is ArcTool arcTool)
        {
            DrawArcToolMeasurementPreview(
                context,
                arcTool);
        }
        else if (_workspace.ToolController.ActiveTool is ArcThreePointsTool arcThreePointsTool)
        {
            DrawArcThreePointsToolMeasurementPreview(
                context,
                arcThreePointsTool);
        }
    }

    private void DrawArcToolMeasurementPreview(
        DrawingContext context,
        ArcTool tool)
    {
        if (tool.CenterPoint is null)
        {
            return;
        }

        const double markerRadius = 4;
        Point center = ToScreenPoint(tool.CenterPoint.Value);

        context.DrawEllipse(
            _basePointMarkerFill,
            _basePointMarkerPen,
            center,
            markerRadius,
            markerRadius);

        if (tool.StartPoint is not null)
        {
            context.DrawLine(
                _measurementVectorPen,
                center,
                ToScreenPoint(tool.StartPoint.Value));
        }

        if (tool.CurrentPoint is null ||
            _workspace?.GeometryTolerance.ArePointsEqual(
                tool.CenterPoint.Value,
                tool.CurrentPoint.Value) == true)
        {
            return;
        }

        context.DrawLine(
            _measurementVectorPen,
            center,
            ToScreenPoint(tool.CurrentPoint.Value));
    }

    private void DrawArcThreePointsToolMeasurementPreview(
        DrawingContext context,
        ArcThreePointsTool tool)
    {
        if (tool.StartPoint is null)
        {
            return;
        }

        const double markerRadius = 4;
        Point start = ToScreenPoint(tool.StartPoint.Value);

        context.DrawEllipse(
            _basePointMarkerFill,
            _basePointMarkerPen,
            start,
            markerRadius,
            markerRadius);

        if (tool.PointOnArc is not null)
        {
            Point pointOnArc = ToScreenPoint(tool.PointOnArc.Value);

            context.DrawEllipse(
                _basePointMarkerFill,
                _basePointMarkerPen,
                pointOnArc,
                markerRadius,
                markerRadius);

            context.DrawLine(
                _measurementVectorPen,
                start,
                pointOnArc);

            if (tool.CurrentPoint is not null)
            {
                context.DrawLine(
                    _measurementVectorPen,
                    pointOnArc,
                    ToScreenPoint(tool.CurrentPoint.Value));
            }
        }
        else if (tool.CurrentPoint is not null)
        {
            context.DrawLine(
                _measurementVectorPen,
                start,
                ToScreenPoint(tool.CurrentPoint.Value));
        }
    }

    private void DrawMoveToolMeasurementPreview(
        DrawingContext context,
        MoveTool tool)
    {
        if (tool.FirstPoint is null)
        {
            return;
        }

        Point start = ToScreenPoint(tool.FirstPoint.Value);
        Point end = tool.CurrentPoint is null
            ? start
            : ToScreenPoint(tool.CurrentPoint.Value);

        const double markerRadius = 4;

        context.DrawEllipse(
            _basePointMarkerFill,
            _basePointMarkerPen,
            start,
            markerRadius,
            markerRadius);

        if (tool.CurrentPoint is null)
        {
            return;
        }

        context.DrawLine(
            _measurementVectorPen,
            start,
            end);
    }

    private void DrawTwoPointToolMeasurementPreview(
        DrawingContext context,
        TwoPointToolBase tool)
    {
        if (tool.FirstPoint is null)
        {
            return;
        }

        Point start = ToScreenPoint(tool.FirstPoint.Value);
        Point end = tool.CurrentPoint is null
            ? start
            : ToScreenPoint(tool.CurrentPoint.Value);

        const double markerRadius = 4;

        context.DrawEllipse(
            _basePointMarkerFill,
            _basePointMarkerPen,
            start,
            markerRadius,
            markerRadius);

        if (tool.CurrentPoint is null ||
            _workspace?.GeometryTolerance.ArePointsEqual(
                tool.FirstPoint.Value,
                tool.CurrentPoint.Value) == true)
        {
            return;
        }

        context.DrawLine(
            _measurementVectorPen,
            start,
            end);

        context.DrawEllipse(
            null,
            _measurementVectorPen,
            end,
            markerRadius,
            markerRadius);
    }


    private void DrawGripEditPreview(
        DrawingContext context,
        GripEditTool tool)
    {
        if (tool.PreviewEntity is not null)
        {
            DrawEntity(
                context,
                tool.PreviewEntity,
                _previewPen);
        }

        DrawGripMeasurementPreview(
            context,
            tool);

        DrawGripMarkers(
            context,
            tool);
    }

    private void DrawGripMeasurementPreview(
        DrawingContext context,
        GripEditTool tool)
    {
        if (_workspace?.Context.CurrentBasePoint is null ||
            tool.CurrentDestination is null)
        {
            return;
        }

        Point start = ToScreenPoint(_workspace.Context.CurrentBasePoint.Value);
        Point end = ToScreenPoint(tool.CurrentDestination.Value);
        const double baseMarkerRadius = 4;
        const double destinationMarkerRadius = 5;

        context.DrawEllipse(
            _basePointMarkerFill,
            _basePointMarkerPen,
            start,
            baseMarkerRadius,
            baseMarkerRadius);

        context.DrawLine(
            _measurementVectorPen,
            start,
            end);

        context.DrawEllipse(
            _basePointMarkerFill,
            _measurementVectorPen,
            end,
            destinationMarkerRadius,
            destinationMarkerRadius);
    }

    private void DrawGripMarkers(
        DrawingContext context,
        GripEditTool tool)
    {
        IReadOnlyList<GripPoint> grips = tool.CurrentGrips;

        for (int i = 0; i < grips.Count; i++)
        {
            Point point = ToScreenPoint(grips[i].Position);

            bool isWarm = tool.WarmGripIndex == i;
            bool isHot = tool.HotGripIndex == i;

            DrawGripMarker(
                context,
                point,
                grips[i].Kind,
                isHot,
                isWarm);
        }
    }

    private void DrawGripMarker(
        DrawingContext context,
        Point center,
        GripKind kind,
        bool isHot,
        bool isWarm)
    {
        double size = isWarm
            ? 13
            : isHot
                ? 11
                : 9;

        double half = size / 2.0;

        var rect = new Rect(
            center.X - half,
            center.Y - half,
            size,
            size);

        IBrush? fill = _gripColdFill;
        Pen pen = _gripColdPen;

        if (isWarm)
        {
            context.DrawEllipse(
                null,
                _gripWarmHaloPen,
                center,
                10,
                10);

            fill = _gripWarmFill;
            pen = _gripWarmPen;
        }
        else if (isHot)
        {
            context.DrawEllipse(
                null,
                _gripHotHaloPen,
                center,
                9,
                9);

            fill = _gripHotFill;
            pen = _gripHotPen;
        }

        if (kind == GripKind.InsertVertex)
        {
            context.DrawEllipse(
                fill,
                pen,
                center,
                half,
                half);
            return;
        }

        context.DrawRectangle(
            fill,
            pen,
            rect);
    }

    private void DrawLinePreview(
        DrawingContext context,
        LineTool tool)
    {
        LineEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawRectanglePreview(
        DrawingContext context,
        RectangleTool tool)
    {
        PolylineEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawRectangleBySidesPreview(
        DrawingContext context,
        RectangleBySidesTool tool)
    {
        LineEntity? firstSidePreview = tool.GetFirstSidePreviewEntity();

        if (firstSidePreview is not null)
        {
            DrawEntity(
                context,
                firstSidePreview,
                _previewPen);
        }

        PolylineEntity? rectanglePreview = tool.GetPreviewEntity();

        if (rectanglePreview is not null)
        {
            DrawEntity(
                context,
                rectanglePreview,
                _previewPen);
        }
    }

    private void DrawCirclePreview(
        DrawingContext context,
        CircleTool tool)
    {
        CircleEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawEllipsePreview(
        DrawingContext context,
        EllipseTool tool)
    {
        EllipseEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawArcPreview(
        DrawingContext context,
        ArcTool tool)
    {
        ArcEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawArcThreePointsPreview(
        DrawingContext context,
        ArcThreePointsTool tool)
    {
        ArcEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawPolylinePreview(
        DrawingContext context,
        PolylineTool tool)
    {
        PolylineEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawSplinePreview(
        DrawingContext context,
        SplineTool tool)
    {
        BezierSplineEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawPolygonPreview(
        DrawingContext context,
        PolygonTool tool)
    {
        PolylineEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawOffsetPreview(
        DrawingContext context,
        OffsetTool tool)
    {
        CadEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }

    private void DrawMirrorPreview(
        DrawingContext context,
        MirrorTool tool)
    {
        if (_workspace is null)
        {
            return;
        }

        DrawEntitiesPreview(
            context,
            tool.GetPreviewEntities(_workspace.Context));

        if (tool.FirstAxisPoint is null)
        {
            return;
        }

        const double markerRadius = 4;
        Point first = ToScreenPoint(tool.FirstAxisPoint.Value);

        context.DrawEllipse(
            _basePointMarkerFill,
            _basePointMarkerPen,
            first,
            markerRadius,
            markerRadius);

        if (tool.SecondAxisPoint is null)
        {
            return;
        }

        Point second = ToScreenPoint(tool.SecondAxisPoint.Value);

        context.DrawLine(
            _measurementVectorPen,
            first,
            second);

        context.DrawEllipse(
            _basePointMarkerFill,
            _basePointMarkerPen,
            second,
            markerRadius,
            markerRadius);
    }

    private void DrawEntitiesPreview(
        DrawingContext context,
        IReadOnlyList<CadEntity> entities)
    {
        DrawEntitiesPreview(
            context,
            entities,
            _previewPen);
    }

    private void DrawEntitiesPreview(
        DrawingContext context,
        IReadOnlyList<CadEntity> entities,
        Pen pen)
    {
        foreach (CadEntity entity in entities)
        {
            DrawEntity(
                context,
                entity,
                pen);
        }
    }

    private void DrawMeasureDistancePreview(
        DrawingContext context,
        MeasureDistanceTool tool)
    {
        LineEntity? preview = tool.GetPreviewEntity();

        if (preview is not null)
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }
    }


    private void DrawMeasureAnglePreview(
        DrawingContext context,
        MeasureAngleTool tool)
    {
        foreach (LineEntity preview in tool.GetPreviewEntities())
        {
            DrawEntity(
                context,
                preview,
                _previewPen);
        }

        const double markerRadius = 4;

        if (tool.FirstRayPoint is not null)
        {
            context.DrawEllipse(
                _basePointMarkerFill,
                _basePointMarkerPen,
                ToScreenPoint(tool.FirstRayPoint.Value),
                markerRadius,
                markerRadius);
        }

        if (tool.Vertex is not null)
        {
            context.DrawEllipse(
                _basePointMarkerFill,
                _basePointMarkerPen,
                ToScreenPoint(tool.Vertex.Value),
                markerRadius,
                markerRadius);
        }
    }

    private void DrawSelectionPreview(
        DrawingContext context,
        SelectionTool tool)
    {
        BoundingBox2D? window = tool.GetPreviewWindow();

        if (window is null)
        {
            return;
        }

        Rect rect = ToScreenRect(window.Value);

        context.DrawRectangle(
            _selectionWindowFill,
            _selectionWindowPen,
            rect);
    }

    private void DrawZoomWindowPreview(
        DrawingContext context,
        ZoomWindowTool tool)
    {
        BoundingBox2D? window = tool.GetPreviewWindow();

        if (window is null)
        {
            return;
        }

        Rect rect = ToScreenRect(window.Value);

        context.DrawRectangle(
            _zoomWindowFill,
            _zoomWindowPen,
            rect);
    }

    private void DrawEntity(
        DrawingContext context,
        CadEntity entity,
        Pen pen,
        bool isSelected = false)
    {
        if (_workspace is null)
        {
            return;
        }

        _entityRenderer.DrawEntity(
            context,
            _workspace,
            entity,
            pen,
            isSelected);
    }


    private Point ToScreenPoint(Point2D point)
    {
        return _viewport.ModelToScreen(point);
    }

    private Rect ToScreenRect(BoundingBox2D box)
    {
        Point topLeft = ToScreenPoint(new Point2D(box.MinX, box.MaxY));
        Point bottomRight = ToScreenPoint(new Point2D(box.MaxX, box.MinY));

        return new Rect(topLeft, bottomRight).Normalize();
    }
}
