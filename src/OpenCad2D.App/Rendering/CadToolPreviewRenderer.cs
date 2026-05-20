using Avalonia;
using Avalonia.Media;
using OpenCad2D.App.Viewport;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
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
/// Interactive tools can provide preview descriptors or preview entities, while
/// this renderer remains responsible only for Avalonia drawing details.
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
    private readonly Pen _removalPreviewHighlightPen = new(
        new SolidColorBrush(Color.FromRgb(255, 90, 90)),
        2.5,
        new DashStyle(new double[] { 6, 4 }, 0));
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

        ICadTool activeTool = _workspace.ToolController.ActiveTool;
        _ = TryDrawToolProvidedDescriptorPreview(
            context,
            activeTool) ||
            TryDrawToolProvidedEntityPreview(
                context,
                activeTool);

        if (activeTool is TwoPointToolBase twoPointTool)
        {
            DrawTwoPointToolMeasurementPreview(
                context,
                twoPointTool);
        }
        else if (activeTool is MoveTool moveTool)
        {
            DrawMoveToolMeasurementPreview(
                context,
                moveTool);
        }
        else if (activeTool is ArcTool arcTool)
        {
            DrawArcToolMeasurementPreview(
                context,
                arcTool);
        }
        else if (activeTool is ArcThreePointsTool arcThreePointsTool)
        {
            DrawArcThreePointsToolMeasurementPreview(
                context,
                arcThreePointsTool);
        }
    }


    private bool TryDrawToolProvidedDescriptorPreview(
        DrawingContext context,
        ICadTool activeTool)
    {
        if (_workspace is null ||
            activeTool is not IToolPreviewDescriptorProvider previewProvider)
        {
            return false;
        }

        DrawPreviewDescriptor(
            context,
            previewProvider.GetPreviewDescriptor(_workspace.Context));

        return true;
    }

    private bool TryDrawToolProvidedEntityPreview(
        DrawingContext context,
        ICadTool activeTool)
    {
        if (_workspace is null ||
            activeTool is not IToolPreviewEntityProvider previewProvider)
        {
            return false;
        }

        DrawEntitiesPreview(
            context,
            previewProvider.GetPreviewEntities(_workspace.Context));

        return true;
    }

    private void DrawPreviewDescriptor(
        DrawingContext context,
        ToolPreviewDescriptor descriptor)
    {
        DrawEntitiesPreview(
            context,
            descriptor.Entities);
        DrawEntitiesPreview(
            context,
            descriptor.HighlightedEntities,
            GetHighlightedEntityPen(descriptor.HighlightedEntityKind));

        foreach (ToolPreviewLine line in descriptor.Lines)
        {
            DrawPreviewLine(
                context,
                line);
        }

        foreach (ToolPreviewMarker marker in descriptor.Markers)
        {
            DrawPreviewMarker(
                context,
                marker);
        }

        foreach (ToolPreviewWindow window in descriptor.Windows)
        {
            DrawPreviewWindow(
                context,
                window);
        }
    }


    private Pen GetHighlightedEntityPen(ToolPreviewHighlightKind kind)
    {
        return kind == ToolPreviewHighlightKind.Removal
            ? _removalPreviewHighlightPen
            : _modifyPreviewHighlightPen;
    }

    private void DrawPreviewLine(
        DrawingContext context,
        ToolPreviewLine line)
    {
        Pen pen = line.Kind == ToolPreviewLineKind.Axis
            ? _measurementVectorPen
            : _measurementVectorPen;

        context.DrawLine(
            pen,
            ToScreenPoint(line.Start),
            ToScreenPoint(line.End));
    }

    private void DrawPreviewMarker(
        DrawingContext context,
        ToolPreviewMarker marker)
    {
        Point center = ToScreenPoint(marker.Position);

        switch (marker.Kind)
        {
        case ToolPreviewMarkerKind.GripCold:
            DrawGripMarker(
                context,
                center,
                marker.Shape,
                isHot: false,
                isWarm: false);
            return;

        case ToolPreviewMarkerKind.GripHot:
            DrawGripMarker(
                context,
                center,
                marker.Shape,
                isHot: true,
                isWarm: false);
            return;

        case ToolPreviewMarkerKind.GripWarm:
            DrawGripMarker(
                context,
                center,
                marker.Shape,
                isHot: false,
                isWarm: true);
            return;
        }

        const double markerRadius = 4;

        context.DrawEllipse(
            _basePointMarkerFill,
            marker.Kind == ToolPreviewMarkerKind.Secondary
                ? _measurementVectorPen
                : _basePointMarkerPen,
            center,
            markerRadius,
            markerRadius);
    }

    private void DrawPreviewWindow(
        DrawingContext context,
        ToolPreviewWindow window)
    {
        Rect rect = ToScreenRect(window.Bounds);

        IBrush fill = window.Kind == ToolPreviewWindowKind.Zoom
            ? _zoomWindowFill
            : _selectionWindowFill;
        Pen pen = window.Kind == ToolPreviewWindowKind.Zoom
            ? _zoomWindowPen
            : _selectionWindowPen;

        context.DrawRectangle(
            fill,
            pen,
            rect);
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


    private void DrawGripMarker(
        DrawingContext context,
        Point center,
        ToolPreviewMarkerShape shape,
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

        if (shape == ToolPreviewMarkerShape.Circle)
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
