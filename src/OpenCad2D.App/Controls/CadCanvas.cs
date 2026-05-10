using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using OpenCad2D.App.Viewport;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Persistence.Dto;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Grips;
using OpenCad2D.Tools.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.Controls;

public sealed class CadCanvas : Control
{
    private readonly Pen _entityPen = new(Brushes.White, 1);
    private readonly Pen _selectedPen = new(Brushes.DeepSkyBlue, 2);
    private readonly Pen _previewPen = new(Brushes.Orange, 1);
    private readonly Pen _measurementVectorPen = new(
        new SolidColorBrush(Color.FromRgb(255, 190, 70)),
        1.5);
    private readonly Pen _basePointMarkerPen = new(
        new SolidColorBrush(Color.FromRgb(255, 220, 120)),
        1.5);
    private readonly IBrush _basePointMarkerFill = new SolidColorBrush(
        Color.FromArgb(80, 255, 190, 70));
    private readonly Pen _selectionWindowPen = new(Brushes.LightGreen, 1);
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
    private readonly IBrush _backgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private readonly IBrush _selectionWindowFill = new SolidColorBrush(Color.FromArgb(35, 80, 180, 255));
    private readonly ViewportTransform _viewport = new();
    private Point? _pointerScreenPoint;
    private bool _isPointerInside;
    private readonly Dictionary<PenCacheKey, Pen> _penCache = new();
    private readonly record struct PenCacheKey(
        byte R,
        byte G,
        byte B,
        double Thickness);
    private const double GridLineDetectionTolerance = 1e-6;
    private bool _isPanning;
    private Point _lastPanScreenPoint;
    private readonly Pen _crosshairPen = new(
        new SolidColorBrush(Color.FromArgb(160, 220, 220, 220)),
        1);
    private readonly Pen _crosshairCenterPen = new(
        new SolidColorBrush(Color.FromRgb(255, 255, 255)),
        1.5);
    private readonly Pen _snapMarkerPen = new(
        new SolidColorBrush(Color.FromRgb(255, 230, 80)),
        2);
    private SnapCandidate? _currentSnapCandidate;
    private readonly Pen _gridMinorPen = new(
        new SolidColorBrush(Color.FromRgb(45, 45, 45)),
        1);
    private readonly Pen _gridMajorPen = new(
        new SolidColorBrush(Color.FromRgb(60, 60, 60)),
        1);
    private readonly Pen _axisXPen = new(
        new SolidColorBrush(Color.FromRgb(120, 70, 70)),
        1.5);
    private readonly Pen _axisYPen = new(
        new SolidColorBrush(Color.FromRgb(70, 120, 70)),
        1.5);
    private readonly Pen _ucsAxisXPen = new(
        new SolidColorBrush(Color.FromRgb(255, 120, 120)),
        2);
    private readonly Pen _ucsAxisYPen = new(
        new SolidColorBrush(Color.FromRgb(120, 255, 120)),
        2);

    public ViewportTransform Viewport => _viewport;

    public int RenderedEntityCount { get; private set; }

    public BoundingBox2D LastVisibleWorldBounds { get; private set; } =
        new(0, 0, 0, 0);

    public static readonly StyledProperty<CadWorkspace?> WorkspaceProperty =
        AvaloniaProperty.Register<CadCanvas, CadWorkspace?>(nameof(Workspace));

    public CadWorkspace? Workspace
    {
        get => GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    public event EventHandler<CadCanvasWorkspaceChangedEventArgs>? WorkspaceChanged;

    public CadCanvas()
    {
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.None);

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        KeyDown += OnKeyDown;
    }

    private void OnPointerEntered(
        object? sender,
        PointerEventArgs e)
    {
        _isPointerInside = true;
        _pointerScreenPoint = e.GetPosition(this);
        InvalidateVisual();
    }

    private void OnPointerExited(
        object? sender,
        PointerEventArgs e)
    {
        _isPointerInside = false;
        _pointerScreenPoint = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(
            _backgroundBrush,
            Bounds);

        DrawGrid(context);
        DrawCurrentUcs(context);

        if (Workspace is null)
        {
            RenderedEntityCount = 0;
            LastVisibleWorldBounds = new BoundingBox2D(0, 0, 0, 0);
            return;
        }

        IReadOnlyList<CadEntity> renderableEntities = GetRenderableEntities();
        RenderedEntityCount = renderableEntities.Count;

        foreach (CadEntity entity in renderableEntities)
        {
            Pen pen = Workspace.SelectionSet.Contains(entity.Id)
                ? _selectedPen
                : GetOrCreateEntityPen(entity);

            DrawEntity(
                context,
                entity,
                pen);
        }

        DrawActiveToolPreview(context);
        DrawCrosshair(context);
        DrawSnapMarker(context);
    }

    private IReadOnlyList<CadEntity> GetRenderableEntities()
    {
        if (Workspace is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            LastVisibleWorldBounds = new BoundingBox2D(0, 0, 0, 0);
            return Array.Empty<CadEntity>();
        }

        const double screenMargin = 24;

        BoundingBox2D visibleWorldBounds = _viewport
            .GetVisibleWorldBounds(new Size(Bounds.Width, Bounds.Height))
            .Expand(_viewport.ScreenLengthToModel(screenMargin));

        LastVisibleWorldBounds = visibleWorldBounds;

        return Workspace.Document
            .GetVisibleEntities(visibleWorldBounds)
            .ToList();
    }

    private void DrawCurrentUcs(DrawingContext context)
    {
        if (Workspace is null)
        {
            return;
        }

        double axisLength = _viewport.ScreenLengthToModel(55);
        Point2D origin = Workspace.CurrentUcs.Origin;
        Point2D xEnd = origin + Workspace.CurrentUcs.XAxis * axisLength;
        Point2D yEnd = origin + Workspace.CurrentUcs.YAxis * axisLength;

        Point screenOrigin = ToScreenPoint(origin);

        context.DrawEllipse(
            Brushes.White,
            null,
            screenOrigin,
            3,
            3);

        context.DrawLine(
            _ucsAxisXPen,
            screenOrigin,
            ToScreenPoint(xEnd));

        context.DrawLine(
            _ucsAxisYPen,
            screenOrigin,
            ToScreenPoint(yEnd));
    }

    private Pen GetOrCreateEntityPen(CadEntity entity)
    {
        CadColor color = ResolveEntityColor(entity);
        const double thickness = 1;

        var key = new PenCacheKey(
            color.R,
            color.G,
            color.B,
            thickness);

        if (_penCache.TryGetValue(key, out Pen? cachedPen))
        {
            return cachedPen;
        }

        var brush = new SolidColorBrush(
            Color.FromRgb(
                color.R,
                color.G,
                color.B));

        var pen = new Pen(
            brush,
            thickness);

        _penCache.Add(key, pen);

        return pen;
    }

    private CadColor ResolveEntityColor(CadEntity entity)
    {
        if (!entity.Style.Color.IsByLayer)
        {
            return entity.Style.Color;
        }

        if (Workspace is null)
        {
            return CadColor.FromRgb(255, 255, 255);
        }

        if (!Workspace.Document.Layers.TryGet(entity.LayerId, out Layer? layer) ||
            layer is null)
        {
            return CadColor.FromRgb(255, 255, 255);
        }

        return layer.Color;
    }

    private void DrawSnapMarker(DrawingContext context)
    {
        if (_currentSnapCandidate is null)
        {
            return;
        }

        Point point = ToScreenPoint(_currentSnapCandidate.Point);

        switch (_currentSnapCandidate.Kind)
        {
            case SnapKind.Endpoint:
                DrawEndpointSnapMarker(context, point);
                break;

            case SnapKind.Midpoint:
                DrawMidpointSnapMarker(context, point);
                break;

            case SnapKind.Center:
                DrawCenterSnapMarker(context, point);
                break;

            case SnapKind.Quadrant:
                DrawQuadrantSnapMarker(context, point);
                break;

            case SnapKind.Intersection:
                DrawIntersectionSnapMarker(context, point);
                break;

            case SnapKind.Nearest:
                DrawNearestSnapMarker(context, point);
                break;

            case SnapKind.Perpendicular:
                DrawPerpendicularSnapMarker(context, point);
                break;

            case SnapKind.Tangent:
                DrawTangentSnapMarker(context, point);
                break;

            case SnapKind.Grid:
                DrawGridSnapMarker(context, point);
                break;

            default:
                DrawDefaultSnapMarker(context, point);
                break;
        }
    }

    private void DrawEndpointSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 8;

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - size, point.Y + size),
            new Point(point.X - size, point.Y - size));

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - size, point.Y + size),
            new Point(point.X + size, point.Y + size));
    }

    private void DrawMidpointSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 7;

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - size, point.Y - size),
            new Point(point.X + size, point.Y + size));

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - size, point.Y + size),
            new Point(point.X + size, point.Y - size));
    }

    private void DrawCenterSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double radius = 7;

        context.DrawEllipse(
            null,
            _snapMarkerPen,
            point,
            radius,
            radius);
    }

    private void DrawQuadrantSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 8;
        Point top = new(point.X, point.Y - size);
        Point right = new(point.X + size, point.Y);
        Point bottom = new(point.X, point.Y + size);
        Point left = new(point.X - size, point.Y);

        context.DrawLine(_snapMarkerPen, top, right);
        context.DrawLine(_snapMarkerPen, right, bottom);
        context.DrawLine(_snapMarkerPen, bottom, left);
        context.DrawLine(_snapMarkerPen, left, top);
    }

    private void DrawIntersectionSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 8;

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - size, point.Y),
            new Point(point.X + size, point.Y));

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X, point.Y - size),
            new Point(point.X, point.Y + size));
    }

    private void DrawNearestSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 6;
        var rect = new Rect(
            point.X - size,
            point.Y - size,
            size * 2,
            size * 2);

        context.DrawRectangle(
            null,
            _snapMarkerPen,
            rect);
    }

    private void DrawPerpendicularSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 8;

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - size, point.Y - size),
            new Point(point.X + size, point.Y - size));

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X, point.Y - size),
            new Point(point.X, point.Y + size));
    }

    private void DrawTangentSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double radius = 6;
        const double line = 10;

        context.DrawEllipse(
            null,
            _snapMarkerPen,
            point,
            radius,
            radius);

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - line, point.Y + radius + 3),
            new Point(point.X + line, point.Y + radius + 3));
    }

    private void DrawGridSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 7;

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X - size, point.Y),
            new Point(point.X + size, point.Y));

        context.DrawLine(
            _snapMarkerPen,
            new Point(point.X, point.Y - size),
            new Point(point.X, point.Y + size));

        var rect = new Rect(
            point.X - 3,
            point.Y - 3,
            6,
            6);

        context.DrawRectangle(
            null,
            _snapMarkerPen,
            rect);
    }

    private void DrawDefaultSnapMarker(
        DrawingContext context,
        Point point)
    {
        const double size = 5;
        var rect = new Rect(
            point.X - size,
            point.Y - size,
            size * 2,
            size * 2);

        context.DrawRectangle(
            null,
            _snapMarkerPen,
            rect);
    }

    private void UpdateCurrentSnapCandidate(Point2D modelPoint)
    {
        if (Workspace is null ||
            Workspace.Context.EnabledSnaps == SnapKind.None ||
            Workspace.Context.SnapTolerance <= 0)
        {
            _currentSnapCandidate = null;
            return;
        }

        Point2D? basePoint = Workspace.Context.CurrentBasePoint;

        var request = new SnapRequest(
            Workspace.Document,
            modelPoint,
            Workspace.Context.SnapTolerance,
            Workspace.Context.EnabledSnaps,
            basePoint,
            Workspace.Context.GridSettings);

        _currentSnapCandidate = Workspace.SnapService.Snap(request);
    }

    private void DrawGrid(DrawingContext context)
    {
        if (Workspace is null)
        {
            return;
        }

        GridSettings grid = Workspace.Context.GridSettings;

        if (!grid.IsVisible)
        {
            return;
        }

        double minorScreenSpacing = _viewport.ModelLengthToScreen(grid.MinorStep);
        double majorScreenSpacing = _viewport.ModelLengthToScreen(grid.MajorStep);
        bool drawMinor = grid.ShouldRenderScreenSpacing(minorScreenSpacing);
        bool drawMajor = grid.ShouldRenderScreenSpacing(majorScreenSpacing);

        if (!drawMinor && !drawMajor)
        {
            return;
        }

        Point2D topLeftModel = ToModelPoint(new Point(0, 0));
        Point2D bottomRightModel = ToModelPoint(new Point(Bounds.Width, Bounds.Height));

        double minX = Math.Min(topLeftModel.X, bottomRightModel.X);
        double maxX = Math.Max(topLeftModel.X, bottomRightModel.X);
        double minY = Math.Min(topLeftModel.Y, bottomRightModel.Y);
        double maxY = Math.Max(topLeftModel.Y, bottomRightModel.Y);

        if (drawMinor)
        {
            DrawGridLines(
                context,
                grid,
                grid.MinorStep,
                _gridMinorPen,
                minX,
                maxX,
                minY,
                maxY,
                skipMajorLines: drawMajor);
        }

        if (drawMajor)
        {
            DrawGridLines(
                context,
                grid,
                grid.MajorStep,
                _gridMajorPen,
                minX,
                maxX,
                minY,
                maxY,
                skipMajorLines: false);
        }

        DrawAxes(
            context,
            minX,
            maxX,
            minY,
            maxY);
    }

    private void DrawGridLines(
        DrawingContext context,
        GridSettings grid,
        double step,
        Pen pen,
        double minX,
        double maxX,
        double minY,
        double maxY,
        bool skipMajorLines)
    {
        double startX = grid.OriginX + Math.Floor((minX - grid.OriginX) / step) * step;
        double endX = grid.OriginX + Math.Ceiling((maxX - grid.OriginX) / step) * step;
        double startY = grid.OriginY + Math.Floor((minY - grid.OriginY) / step) * step;
        double endY = grid.OriginY + Math.Ceiling((maxY - grid.OriginY) / step) * step;

        for (double x = startX; x <= endX; x += step)
        {
            if (skipMajorLines && IsGridCoordinateMultipleOf(x, grid.OriginX, grid.MajorStep))
            {
                continue;
            }

            context.DrawLine(
                pen,
                ToScreenPoint(new Point2D(x, minY)),
                ToScreenPoint(new Point2D(x, maxY)));
        }

        for (double y = startY; y <= endY; y += step)
        {
            if (skipMajorLines && IsGridCoordinateMultipleOf(y, grid.OriginY, grid.MajorStep))
            {
                continue;
            }

            context.DrawLine(
                pen,
                ToScreenPoint(new Point2D(minX, y)),
                ToScreenPoint(new Point2D(maxX, y)));
        }
    }

    private void DrawAxes(
        DrawingContext context,
        double minX,
        double maxX,
        double minY,
        double maxY)
    {
        if (minY <= 0 && maxY >= 0)
        {
            context.DrawLine(
                _axisXPen,
                ToScreenPoint(new Point2D(minX, 0)),
                ToScreenPoint(new Point2D(maxX, 0)));
        }

        if (minX <= 0 && maxX >= 0)
        {
            context.DrawLine(
                _axisYPen,
                ToScreenPoint(new Point2D(0, minY)),
                ToScreenPoint(new Point2D(0, maxY)));
        }
    }

    private double GetNiceGridStep()
    {
        const double targetScreenSpacing = 40.0;
        double rawModelStep = _viewport.ScreenLengthToModel(targetScreenSpacing);

        if (rawModelStep <= 0)
        {
            return 10;
        }

        double exponent = Math.Floor(Math.Log10(rawModelStep));
        double magnitude = Math.Pow(10, exponent);
        double normalized = rawModelStep / magnitude;

        double niceNormalized;

        if (normalized <= 1)
        {
            niceNormalized = 1;
        }
        else if (normalized <= 2)
        {
            niceNormalized = 2;
        }
        else if (normalized <= 5)
        {
            niceNormalized = 5;
        }
        else
        {
            niceNormalized = 10;
        }

        return niceNormalized * magnitude;
    }

    private static bool IsMultipleOf(
        double value,
        double step)
    {
        return IsGridCoordinateMultipleOf(
            value,
            origin: 0,
            step);
    }

    private static bool IsGridCoordinateMultipleOf(
        double value,
        double origin,
        double step)
    {
        if (step <= 0)
        {
            return false;
        }

        double quotient = (value - origin) / step;

        return Tolerance.AreEqual(
            quotient,
            Math.Round(quotient),
            GridLineDetectionTolerance);
    }

    private void NotifyWorkspaceChanged(
        ToolResult result,
        Point2D mousePosition)
    {
        WorkspaceChanged?.Invoke(
            this,
            new CadCanvasWorkspaceChangedEventArgs(
                result,
                mousePosition,
                _currentSnapCandidate));
    }

    private void DrawActiveToolPreview(DrawingContext context)
    {
        if (Workspace is null)
        {
            return;
        }

        switch (Workspace.ToolController.ActiveTool)
        {
            case LineTool lineTool:
                DrawLinePreview(context, lineTool);
                break;

            case RectangleTool rectangleTool:
                DrawRectanglePreview(context, rectangleTool);
                break;

            case CircleTool circleTool:
                DrawCirclePreview(context, circleTool);
                break;

            case PolylineTool polylineTool:
                DrawPolylinePreview(context, polylineTool);
                break;

            case MoveTool moveTool:
                DrawEntitiesPreview(
                    context,
                    moveTool.GetPreviewEntities(Workspace.Context));
                break;

            case CopyTool copyTool:
                DrawEntitiesPreview(
                    context,
                    copyTool.GetPreviewEntities(Workspace.Context));
                break;

            case RotateTool rotateTool:
                DrawEntitiesPreview(
                    context,
                    rotateTool.GetPreviewEntities(Workspace.Context));
                break;

            case ScaleTool scaleTool:
                DrawEntitiesPreview(
                    context,
                    scaleTool.GetPreviewEntities(Workspace.Context));
                break;

            case AlignTool alignTool:
                DrawEntitiesPreview(
                    context,
                    alignTool.GetPreviewEntities(Workspace.Context));
                break;

            case SelectionTool selectionTool:
                DrawSelectionPreview(context, selectionTool);
                break;

            case GripEditTool gripEditTool:
                DrawGripEditPreview(context, gripEditTool);
                break;
        }

        if (Workspace.ToolController.ActiveTool is TwoPointToolBase twoPointTool)
        {
            DrawTwoPointToolMeasurementPreview(
                context,
                twoPointTool);
        }
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
            Workspace?.GeometryTolerance.ArePointsEqual(
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
        if (Workspace?.Context.CurrentBasePoint is null ||
            tool.CurrentDestination is null)
        {
            return;
        }

        Point start = ToScreenPoint(Workspace.Context.CurrentBasePoint.Value);
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
                isHot,
                isWarm);
        }
    }

    private void DrawGripMarker(
        DrawingContext context,
        Point center,
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

    private void DrawEntitiesPreview(
        DrawingContext context,
        IReadOnlyList<CadEntity> entities)
    {
        foreach (CadEntity entity in entities)
        {
            DrawEntity(
                context,
                entity,
                _previewPen);
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

    private void DrawEntity(
        DrawingContext context,
        CadEntity entity,
        Pen pen)
    {
        switch (entity)
        {
            case LineEntity line:
                context.DrawLine(
                    pen,
                    ToScreenPoint(line.Start),
                    ToScreenPoint(line.End));
                break;

            case CircleEntity circle:
                context.DrawEllipse(
                    null,
                    pen,
                    ToScreenPoint(circle.Center),
                    _viewport.ModelLengthToScreen(circle.Radius),
                    _viewport.ModelLengthToScreen(circle.Radius));
                break;

            case ArcEntity arc:
                DrawArc(
                    context,
                    arc,
                    pen);
                break;

            case PolylineEntity polyline:
                DrawPolyline(
                    context,
                    polyline,
                    pen);
                break;
        }
    }

    private void DrawPolyline(
        DrawingContext context,
        PolylineEntity polyline,
        Pen pen)
    {
        IReadOnlyList<Point2D> vertices = polyline.Vertices;

        for (int i = 0; i < vertices.Count - 1; i++)
        {
            context.DrawLine(
                pen,
                ToScreenPoint(vertices[i]),
                ToScreenPoint(vertices[i + 1]));
        }

        if (polyline.IsClosed && vertices.Count > 1)
        {
            context.DrawLine(
                pen,
                ToScreenPoint(vertices[^1]),
                ToScreenPoint(vertices[0]));
        }
    }

    private void DrawArc(
        DrawingContext context,
        ArcEntity arc,
        Pen pen)
    {
        const int segments = 48;

        Point2D previous = arc.Geometry.StartPoint;
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;

        double sweep = arc.IsCounterClockwise
            ? end - start
            : start - end;

        if (sweep < 0)
        {
            sweep += Math.PI * 2;
        }

        for (int i = 1; i <= segments; i++)
        {
            double t = i / (double)segments;
            double angle = arc.IsCounterClockwise
                ? start + sweep * t
                : start - sweep * t;

            Point2D current = arc.Geometry.PointAt(
                Angle.FromRadians(angle));

            context.DrawLine(
                pen,
                ToScreenPoint(previous),
                ToScreenPoint(current));

            previous = current;
        }
    }

    private void OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (Workspace is null)
        {
            return;
        }

        Focus();

        Point position = e.GetPosition(this);
        _isPointerInside = true;
        _pointerScreenPoint = position;

        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _lastPanScreenPoint = position;
            e.Pointer.Capture(this);
            return;
        }

        Point2D modelPoint = ToModelPoint(position);
        UpdateCurrentSnapCandidate(modelPoint);

        ToolResult result = Workspace.ToolController.OnPointerPressed(
            CreatePointerInfo(
                position,
                e.KeyModifiers));

        NotifyWorkspaceChanged(
            result,
            modelPoint);

        InvalidateVisual();
    }

    private void OnPointerMoved(
        object? sender,
        PointerEventArgs e)
    {
        if (Workspace is null)
        {
            return;
        }

        Point position = e.GetPosition(this);
        _isPointerInside = true;
        _pointerScreenPoint = position;

        if (_isPanning)
        {
            Vector delta = position - _lastPanScreenPoint;
            _viewport.Pan(delta);
            _lastPanScreenPoint = position;

            Point2D modelPoint = ToModelPoint(position);
            _currentSnapCandidate = null;

            NotifyWorkspaceChanged(
                ToolResult.Updated("Pan."),
                modelPoint);

            InvalidateVisual();
            return;
        }

        Point2D point = ToModelPoint(position);
        UpdateCurrentSnapCandidate(point);

        ToolResult result = Workspace.ToolController.OnPointerMoved(
            CreatePointerInfo(
                position,
                e.KeyModifiers));

        NotifyWorkspaceChanged(
            result,
            point);

        InvalidateVisual();
    }

    private void OnPointerReleased(
        object? sender,
        PointerReleasedEventArgs e)
    {
        if (Workspace is null)
        {
            return;
        }

        Point position = e.GetPosition(this);
        _isPointerInside = true;
        _pointerScreenPoint = position;

        Point2D modelPoint = ToModelPoint(position);
        UpdateCurrentSnapCandidate(modelPoint);

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);

            NotifyWorkspaceChanged(
                ToolResult.Updated("Pan completed."),
                modelPoint);

            InvalidateVisual();
            return;
        }

        ToolResult result = Workspace.ToolController.OnPointerReleased(
            CreatePointerInfo(
                position,
                e.KeyModifiers));

        NotifyWorkspaceChanged(
            result,
            modelPoint);

        InvalidateVisual();
    }

    private void OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (Workspace is null)
        {
            return;
        }

        ToolResult? result = null;

        if (e.Key == Key.Escape)
        {
            result = Workspace.Escape();

            ClearSnapMarker();

            e.Handled = true;
        }
        else if (Workspace.ToolController.ActiveTool is AlignTool alignTool &&
                 alignTool.State == AlignToolState.WaitingForScaleConfirmation &&
                 e.Key == Key.Enter)
        {
            result = alignTool.ConfirmWithoutScale(Workspace.Context);
            ClearSnapMarker();
            e.Handled = true;
        }
        else if (Workspace.ToolController.ActiveTool is AlignTool alignToolWithScale &&
                 alignToolWithScale.State == AlignToolState.WaitingForScaleConfirmation &&
                 e.Key == Key.S)
        {
            result = alignToolWithScale.ConfirmWithScale(Workspace.Context);
            ClearSnapMarker();
            e.Handled = true;
        }
        else if (Workspace.ToolController.ActiveTool is PolylineTool polylineTool &&
                 polylineTool.State == PolylineToolState.CollectingVertices &&
                 e.Key == Key.Enter)
        {
            result = polylineTool.CompleteOpen(Workspace.Context);
            ClearSnapMarker();
            e.Handled = true;
        }
        else if (Workspace.ToolController.ActiveTool is PolylineTool closingPolylineTool &&
                 closingPolylineTool.State == PolylineToolState.CollectingVertices &&
                 e.Key == Key.C)
        {
            result = closingPolylineTool.CompleteClosed(Workspace.Context);
            ClearSnapMarker();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            result = Workspace.ActionController.DeleteSelection();
        }
        else if (e.Key == Key.Z &&
                 e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            result = Workspace.ActionController.Undo();
        }
        else if (e.Key == Key.Y &&
                 e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            result = Workspace.ActionController.Redo();
        }
        else if (e.Key == Key.Tab)
        {
            result = Workspace.EnterGripEditModeForSelection();
            e.Handled = result.Changed;
        }
        else if (e.Key == Key.Home)
        {
            result = ZoomExtents();
        }

        if (result is not null)
        {
            NotifyWorkspaceChanged(
                result,
                Point2D.Origin);

            InvalidateVisual();
        }
    }

    private static PointerModifiers GetModifiers(KeyModifiers modifiers)
    {
        PointerModifiers result = PointerModifiers.None;

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= PointerModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= PointerModifiers.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= PointerModifiers.Alt;
        }

        return result;
    }

    private void OnPointerWheelChanged(
        object? sender,
        PointerWheelEventArgs e)
    {
        Point screenPoint = e.GetPosition(this);
        _isPointerInside = true;
        _pointerScreenPoint = screenPoint;

        double zoomFactor = e.Delta.Y > 0
            ? 1.15
            : 1.0 / 1.15;

        _viewport.ZoomAt(
            screenPoint,
            zoomFactor);

        Point2D modelPoint = ToModelPoint(screenPoint);

        NotifyWorkspaceChanged(
            ToolResult.Updated($"Zoom: {_viewport.Scale:0.###}x"),
            modelPoint);

        InvalidateVisual();
    }

    private void DrawCrosshair(DrawingContext context)
    {
        if (!_isPointerInside ||
            _pointerScreenPoint is null)
        {
            return;
        }

        Point point = _pointerScreenPoint.Value;
        double width = Bounds.Width;
        double height = Bounds.Height;

        context.DrawLine(
            _crosshairPen,
            new Point(0, point.Y),
            new Point(width, point.Y));

        context.DrawLine(
            _crosshairPen,
            new Point(point.X, 0),
            new Point(point.X, height));

        const double boxHalfSize = 5;
        var centerBox = new Rect(
            point.X - boxHalfSize,
            point.Y - boxHalfSize,
            boxHalfSize * 2,
            boxHalfSize * 2);

        context.DrawRectangle(
            null,
            _crosshairCenterPen,
            centerBox);
    }

    private Point ToScreenPoint(Point2D point)
    {
        return _viewport.ModelToScreen(point);
    }

    private Point2D ToModelPoint(Point screenPoint)
    {
        return _viewport.ScreenToModel(screenPoint);
    }

    private Rect ToScreenRect(BoundingBox2D box)
    {
        Point topLeft = _viewport.ModelToScreen(
            new Point2D(box.MinX, box.MinY));

        Point bottomRight = _viewport.ModelToScreen(
            new Point2D(box.MaxX, box.MaxY));

        return new Rect(
            topLeft,
            bottomRight);
    }

    public ToolResult ZoomExtents()
    {
        if (Workspace is null)
        {
            return ToolResult.None("No workspace available.");
        }

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return ToolResult.None("Cannot zoom extents because the canvas has no size yet.");
        }

        BoundingBox2D? visibleBounds = GetVisibleEntityBounds();

        if (visibleBounds is null)
        {
            _viewport.Reset();
            InvalidateVisual();

            return ToolResult.Updated("View reset. No visible entities for zoom extents.");
        }

        _viewport.ZoomToFit(
            visibleBounds.Value,
            new Size(Bounds.Width, Bounds.Height),
            screenPadding: 48);

        InvalidateVisual();

        return ToolResult.Updated("Zoom extents applied.");
    }

    private BoundingBox2D? GetVisibleEntityBounds()
    {
        if (Workspace is null)
        {
            return null;
        }

        BoundingBox2D? result = null;

        foreach (CadEntity entity in Workspace.Document.GetVisibleEntities())
        {
            BoundingBox2D entityBounds = entity.GetBoundingBox();

            result = result is null
                ? entityBounds
                : Union(result.Value, entityBounds);
        }

        return result;
    }

    private static BoundingBox2D Union(
        BoundingBox2D first,
        BoundingBox2D second)
    {
        return new BoundingBox2D(
            Math.Min(first.MinX, second.MinX),
            Math.Min(first.MinY, second.MinY),
            Math.Max(first.MaxX, second.MaxX),
            Math.Max(first.MaxY, second.MaxY));
    }

    public ViewportStateDto GetViewportState()
    {
        return new ViewportStateDto
        {
            PanX = _viewport.Offset.X,
            PanY = _viewport.Offset.Y,
            Zoom = _viewport.Scale
        };
    }

    public void ApplyViewportState(ViewportStateDto? viewportState)
    {
        if (viewportState is null)
        {
            ResetViewport();
            return;
        }

        _viewport.SetState(
            viewportState.Zoom,
            new Vector(
                viewportState.PanX,
                viewportState.PanY));

        InvalidateVisual();
    }

    public void ResetViewport()
    {
        _viewport.Reset();
        InvalidateVisual();
    }

    public void ClearSnapMarker()
    {
        _currentSnapCandidate = null;

        NotifyWorkspaceChanged(
            ToolResult.None(),
            Point2D.Origin);

        InvalidateVisual();
    }

    private PointerInfo CreatePointerInfo(
        Point screenPoint,
        KeyModifiers keyModifiers)
    {
        Point2D modelPoint = ToModelPoint(screenPoint);
        Point2D userPoint = Workspace is null
            ? modelPoint
            : Workspace.CurrentUcs.WorldToUser(modelPoint);

        return new PointerInfo(
            modelPoint,
            userPoint,
            GetModifiers(keyModifiers));
    }
}
