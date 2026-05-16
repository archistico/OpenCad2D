using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using OpenCad2D.App.Rendering;
using OpenCad2D.App.Viewport;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Dimensions.Rendering;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Persistence.Dto;
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace OpenCad2D.App.Controls;

public sealed class CadCanvas : Control
{
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
    private readonly IBrush _zoomWindowFill = new SolidColorBrush(Color.FromArgb(30, 80, 210, 255));
    private readonly ViewportTransform _viewport = new();
    private readonly DimensionGeometryBuilder _dimensionGeometryBuilder = new();
    private Point? _pointerScreenPoint;
    private bool _isPointerInside;
    private readonly Dictionary<PenCacheKey, Pen> _penCache = new();
    private readonly record struct PenCacheKey(
        byte R,
        byte G,
        byte B,
        double Thickness,
        LineStyle LineStyle,
        double Scale);
    private const double GridLineDetectionTolerance = 1e-6;
    private bool _isPanning;
    private bool _isTextInputDialogOpen;
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

    public event EventHandler? RepeatLastCommandRequested;

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
            bool isSelected = Workspace.SelectionSet.Contains(entity.Id);
            Pen pen = GetOrCreateEntityPen(
                entity,
                isSelected);

            DrawEntity(
                context,
                entity,
                pen,
                isSelected);
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

    private Pen GetOrCreateEntityPen(
        CadEntity entity,
        bool isSelected)
    {
        if (Workspace is null)
        {
            return new Pen(Brushes.White, LineFormatCollection.Default.GetById(LineFormatId.Continuous).LineWeight.Millimeters);
        }

        EntityScreenStyle screenStyle = EntityScreenStyleResolver.Resolve(
            Workspace.Document,
            entity,
            isSelected);

        CadColor color = screenStyle.Color;
        double thickness = screenStyle.LineWeight;

        var key = new PenCacheKey(
            color.R,
            color.G,
            color.B,
            thickness,
            screenStyle.LineStyle,
            _viewport.Scale);

        if (_penCache.TryGetValue(key, out Pen? cachedPen))
        {
            return cachedPen;
        }

        var brush = new SolidColorBrush(
            Color.FromRgb(
                color.R,
                color.G,
                color.B));

        DashStyle? dashStyle = CreateDashStyle(screenStyle.LineStyle);

        var pen = new Pen(
            brush,
            thickness,
            dashStyle);

        _penCache.Add(key, pen);

        return pen;
    }

    private DashStyle? CreateDashStyle(LineStyle lineStyle)
    {
        double[]? modelPattern = LineStyleDashPattern.Get(lineStyle);

        if (modelPattern is null || modelPattern.Length == 0)
        {
            return null;
        }

        double[] screenPattern = modelPattern
            .Select(_viewport.ModelLengthToScreen)
            .Select(value => Math.Max(0.1, value))
            .ToArray();

        return new DashStyle(
            screenPattern,
            0);
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

            case SnapKind.Entity:
                DrawEntitySnapMarker(context, point);
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

    private void DrawEntitySnapMarker(
        DrawingContext context,
        Point point)
    {
        const double width = 18;
        const double height = 12;

        var rect = new Rect(
            point.X - width / 2,
            point.Y - height / 2,
            width,
            height);

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
            Workspace.Context.SnapTolerance <= 0)
        {
            _currentSnapCandidate = null;
            return;
        }

        SnapKind enabledSnaps = GetEffectiveEnabledSnaps();

        if (enabledSnaps == SnapKind.None)
        {
            _currentSnapCandidate = null;
            return;
        }

        Point2D? basePoint = Workspace.Context.CurrentBasePoint;

        var request = new SnapRequest(
            Workspace.Document,
            modelPoint,
            Workspace.Context.SnapTolerance,
            enabledSnaps,
            basePoint,
            Workspace.Context.GridSettings);

        _currentSnapCandidate = Workspace.SnapService.Snap(request);
    }

    private SnapKind GetEffectiveEnabledSnaps()
    {
        if (Workspace is null)
        {
            return SnapKind.None;
        }

        if (Workspace.ToolController.ActiveTool is ISnapModeProvider provider)
        {
            return provider.GetActiveSnapKind(Workspace.Context);
        }

        return Workspace.Context.EnabledSnaps;
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

        if (grid.Kind == GridKind.Isometric)
        {
            DrawIsometricGrid(
                context,
                grid,
                minX,
                maxX,
                minY,
                maxY,
                drawMinor,
                drawMajor);
        }
        else
        {
            DrawRectangularGrid(
                context,
                grid,
                minX,
                maxX,
                minY,
                maxY,
                drawMinor,
                drawMajor);
        }

        DrawAxes(
            context,
            minX,
            maxX,
            minY,
            maxY);
    }

    private void DrawRectangularGrid(
        DrawingContext context,
        GridSettings grid,
        double minX,
        double maxX,
        double minY,
        double maxY,
        bool drawMinor,
        bool drawMajor)
    {
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
    }

    private void DrawIsometricGrid(
        DrawingContext context,
        GridSettings grid,
        double minX,
        double maxX,
        double minY,
        double maxY,
        bool drawMinor,
        bool drawMajor)
    {
        if (drawMinor)
        {
            DrawIsometricGridLines(
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
            DrawIsometricGridLines(
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
    }

    private void DrawIsometricGridLines(
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
        double angleRadians = grid.IsometricAngleDegrees * Math.PI / 180.0;
        double tangent = Math.Tan(angleRadians);
        double extension = Math.Max(maxX - minX, maxY - minY) * 2 + grid.MajorStep * 4;
        double drawMinX = minX - extension;
        double drawMaxX = maxX + extension;
        double drawMinY = minY - extension;
        double drawMaxY = maxY + extension;

        double verticalStep = grid.GetIsometricVerticalStep(step);
        double majorVerticalStep = grid.GetIsometricVerticalStep(grid.MajorStep);
        double startX = grid.OriginX + Math.Floor((drawMinX - grid.OriginX) / verticalStep) * verticalStep;
        double endX = grid.OriginX + Math.Ceiling((drawMaxX - grid.OriginX) / verticalStep) * verticalStep;

        for (double x = startX; x <= endX; x += verticalStep)
        {
            if (skipMajorLines && IsGridCoordinateMultipleOf(x, grid.OriginX, majorVerticalStep))
            {
                continue;
            }

            context.DrawLine(
                pen,
                ToScreenPoint(new Point2D(x, drawMinY)),
                ToScreenPoint(new Point2D(x, drawMaxY)));
        }

        DrawIsometricDiagonalFamily(
            context,
            grid,
            step,
            pen,
            tangent,
            drawMinX,
            drawMaxX,
            drawMinY,
            drawMaxY,
            positiveSlope: true,
            skipMajorLines: skipMajorLines);

        DrawIsometricDiagonalFamily(
            context,
            grid,
            step,
            pen,
            tangent,
            drawMinX,
            drawMaxX,
            drawMinY,
            drawMaxY,
            positiveSlope: false,
            skipMajorLines: skipMajorLines);
    }

    private void DrawIsometricDiagonalFamily(
        DrawingContext context,
        GridSettings grid,
        double step,
        Pen pen,
        double tangent,
        double minX,
        double maxX,
        double minY,
        double maxY,
        bool positiveSlope,
        bool skipMajorLines)
    {
        double minIntercept = double.PositiveInfinity;
        double maxIntercept = double.NegativeInfinity;

        Point2D[] corners =
        {
            new(minX, minY),
            new(minX, maxY),
            new(maxX, minY),
            new(maxX, maxY)
        };

        foreach (Point2D corner in corners)
        {
            double intercept = positiveSlope
                ? corner.Y - grid.OriginY - tangent * (corner.X - grid.OriginX)
                : corner.Y - grid.OriginY + tangent * (corner.X - grid.OriginX);

            minIntercept = Math.Min(minIntercept, intercept);
            maxIntercept = Math.Max(maxIntercept, intercept);
        }

        double startIntercept = Math.Floor(minIntercept / step) * step;
        double endIntercept = Math.Ceiling(maxIntercept / step) * step;

        for (double intercept = startIntercept; intercept <= endIntercept; intercept += step)
        {
            if (skipMajorLines && IsGridCoordinateMultipleOf(intercept, 0, grid.MajorStep))
            {
                continue;
            }

            double startY = positiveSlope
                ? grid.OriginY + tangent * (minX - grid.OriginX) + intercept
                : grid.OriginY - tangent * (minX - grid.OriginX) + intercept;

            double endY = positiveSlope
                ? grid.OriginY + tangent * (maxX - grid.OriginX) + intercept
                : grid.OriginY - tangent * (maxX - grid.OriginX) + intercept;

            context.DrawLine(
                pen,
                ToScreenPoint(new Point2D(minX, startY)),
                ToScreenPoint(new Point2D(maxX, endY)));
        }
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

            case RectangleBySidesTool rectangleBySidesTool:
                DrawRectangleBySidesPreview(context, rectangleBySidesTool);
                break;

            case CircleTool circleTool:
                DrawCirclePreview(context, circleTool);
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

        if (Workspace.ToolController.ActiveTool is TwoPointToolBase twoPointTool)
        {
            DrawTwoPointToolMeasurementPreview(
                context,
                twoPointTool);
        }
        else if (Workspace.ToolController.ActiveTool is MoveTool moveTool)
        {
            DrawMoveToolMeasurementPreview(
                context,
                moveTool);
        }
        else if (Workspace.ToolController.ActiveTool is ArcTool arcTool)
        {
            DrawArcToolMeasurementPreview(
                context,
                arcTool);
        }
        else if (Workspace.ToolController.ActiveTool is ArcThreePointsTool arcThreePointsTool)
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
            Workspace?.GeometryTolerance.ArePointsEqual(
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
        switch (entity)
        {
            case PointEntity point:
                DrawPoint(
                    context,
                    point,
                    pen);
                break;

            case TextEntity text:
                DrawText(
                    context,
                    text,
                    pen,
                    isSelected);
                break;

            case LinearDimensionEntity linearDimension:
                DrawDimension(
                    context,
                    linearDimension,
                    pen,
                    isSelected);
                break;

            case AlignedDimensionEntity alignedDimension:
                DrawDimension(
                    context,
                    alignedDimension,
                    pen,
                    isSelected);
                break;

            case RadiusDimensionEntity radiusDimension:
                DrawDimension(
                    context,
                    radiusDimension,
                    pen,
                    isSelected);
                break;

            case DiameterDimensionEntity diameterDimension:
                DrawDimension(
                    context,
                    diameterDimension,
                    pen,
                    isSelected);
                break;

            case AngularDimensionEntity angularDimension:
                DrawDimension(
                    context,
                    angularDimension,
                    pen,
                    isSelected);
                break;

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


    private void DrawDimension(
        DrawingContext context,
        DimensionEntity dimension,
        Pen pen,
        bool isSelected)
    {
        if (Workspace is null)
        {
            return;
        }

        DimensionStyle style = ResolveDimensionStyle(dimension);
        DimensionRenderModel model = _dimensionGeometryBuilder.Build(
            dimension,
            style);

        foreach (DimensionLinePrimitive line in model.Lines)
        {
            context.DrawLine(
                pen,
                ToScreenPoint(line.Start),
                ToScreenPoint(line.End));
        }

        foreach (DimensionArcPrimitive arc in model.Arcs)
        {
            DrawDimensionArc(
                context,
                arc,
                pen);
        }

        foreach (DimensionLinePrimitive arrow in model.Arrows)
        {
            context.DrawLine(
                pen,
                ToScreenPoint(arrow.Start),
                ToScreenPoint(arrow.End));
        }

        DrawDimensionText(
            context,
            model.Text,
            style,
            pen,
            isSelected);
    }


    private void DrawDimensionArc(
        DrawingContext context,
        DimensionArcPrimitive arc,
        Pen pen)
    {
        const int segments = 48;

        Point2D previous = arc.StartPoint;
        double start = arc.StartAngleDegrees * Math.PI / 180.0;
        double end = arc.EndAngleDegrees * Math.PI / 180.0;

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

            Point2D current = new(
                arc.Center.X + Math.Cos(angle) * arc.Radius,
                arc.Center.Y + Math.Sin(angle) * arc.Radius);

            context.DrawLine(
                pen,
                ToScreenPoint(previous),
                ToScreenPoint(current));

            previous = current;
        }
    }

    private void DrawDimensionText(
        DrawingContext context,
        DimensionTextPrimitive text,
        DimensionStyle style,
        Pen pen,
        bool isSelected)
    {
        if (Workspace is null)
        {
            return;
        }

        TextFormat format = ResolveDimensionTextFormat(style);
        Point insertionPoint = ToScreenPoint(text.Position);
        double fontSize = Math.Max(1.0, _viewport.ModelLengthToScreen(format.Height));

        IBrush brush = isSelected && pen.Brush is not null
            ? pen.Brush
            : new SolidColorBrush(
                Color.FromRgb(
                    format.Color.R,
                    format.Color.G,
                    format.Color.B));

        var typeface = new Typeface(
            new FontFamily(format.FontFamily),
            format.IsItalic ? FontStyle.Italic : FontStyle.Normal,
            format.IsBold ? FontWeight.Bold : FontWeight.Normal);

        var formattedText = new FormattedText(
            text.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            brush);

        using (context.PushTransform(CadTextTransform.CreateCadRotationAt(
                   text.RotationDegrees,
                   insertionPoint.X,
                   insertionPoint.Y)))
        {
            context.DrawText(
                formattedText,
                insertionPoint);
        }
    }

    private DimensionStyle ResolveDimensionStyle(DimensionEntity dimension)
    {
        if (Workspace is not null &&
            Workspace.Document.DimensionStyles.TryGetById(
                dimension.DimensionStyleId,
                out DimensionStyle? style) &&
            style is not null)
        {
            return style;
        }

        return DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard);
    }

    private TextFormat ResolveDimensionTextFormat(DimensionStyle style)
    {
        if (Workspace is not null &&
            Workspace.Document.TextFormats.TryGetById(
                style.TextFormatId,
                out TextFormat? format) &&
            format is not null)
        {
            return format;
        }

        return TextFormatCollection.Default.GetById(TextFormatId.Annotation);
    }

    private void DrawText(
        DrawingContext context,
        TextEntity text,
        Pen pen,
        bool isSelected)
    {
        if (Workspace is null)
        {
            return;
        }

        TextFormat format = ResolveTextFormat(text);
        Point insertionPoint = ToScreenPoint(text.InsertionPoint);
        double fontSize = Math.Max(1.0, _viewport.ModelLengthToScreen(format.Height));

        IBrush brush = isSelected && pen.Brush is not null
            ? pen.Brush
            : new SolidColorBrush(
                Color.FromRgb(
                    format.Color.R,
                    format.Color.G,
                    format.Color.B));

        var typeface = new Typeface(
            new FontFamily(format.FontFamily),
            format.IsItalic ? FontStyle.Italic : FontStyle.Normal,
            format.IsBold ? FontWeight.Bold : FontWeight.Normal);

        var formattedText = new FormattedText(
            text.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            brush);

        using (context.PushTransform(CadTextTransform.CreateCadRotationAt(
                   text.RotationDegrees,
                   insertionPoint.X,
                   insertionPoint.Y)))
        {
            context.DrawText(
                formattedText,
                insertionPoint);
        }
    }

    private TextFormat ResolveTextFormat(TextEntity text)
    {
        if (Workspace is not null &&
            Workspace.Document.TextFormats.TryGetById(
                text.TextFormatId,
                out TextFormat? format) &&
            format is not null)
        {
            return format;
        }

        return TextFormatCollection.Default.GetById(TextFormatId.Standard);
    }

    private void DrawPoint(
        DrawingContext context,
        PointEntity point,
        Pen pen)
    {
        Point center = ToScreenPoint(point.Position);
        const double markerSize = 5.0;

        context.DrawLine(
            pen,
            new Point(center.X - markerSize, center.Y),
            new Point(center.X + markerSize, center.Y));

        context.DrawLine(
            pen,
            new Point(center.X, center.Y - markerSize),
            new Point(center.X, center.Y + markerSize));
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

    private async void OnPointerPressed(
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

        PointerPointProperties pointerProperties = e.GetCurrentPoint(this).Properties;

        if (pointerProperties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _lastPanScreenPoint = position;
            e.Pointer.Capture(this);
            return;
        }

        if (pointerProperties.IsRightButtonPressed)
        {
            RepeatLastCommandRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        Point2D modelPoint = ToModelPoint(position);
        UpdateCurrentSnapCandidate(modelPoint);

        if (_isTextInputDialogOpen)
        {
            return;
        }

        ToolResult result;

        if (Workspace.ToolController.ActiveTool is IAsyncCadTool)
        {
            _isTextInputDialogOpen = true;

            try
            {
                result = await Workspace.ToolController.OnPointerPressedAsync(
                    CreatePointerInfo(
                        position,
                        e.KeyModifiers));
            }
            finally
            {
                _isTextInputDialogOpen = false;
                Focus();
            }
        }
        else
        {
            result = Workspace.ToolController.OnPointerPressed(
                CreatePointerInfo(
                    position,
                    e.KeyModifiers));
        }

        result = ApplyZoomWindowIfCompleted(result);

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

        result = ApplyZoomWindowIfCompleted(result);

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
        else if (Workspace.ToolController.ActiveTool is MoveTool moveTool &&
                 moveTool.MoveState == MoveToolState.WaitingForEntitySelection &&
                 e.Key == Key.Enter)
        {
            result = moveTool.ConfirmEntitySelection(Workspace.Context);
            ClearSnapMarker();
            e.Handled = result.Changed;
        }
        else if (Workspace.ToolController.ActiveTool is CopyTool copyTool &&
                 copyTool.CopyState == MoveToolState.WaitingForEntitySelection &&
                 e.Key == Key.Enter)
        {
            result = copyTool.ConfirmEntitySelection(Workspace.Context);
            ClearSnapMarker();
            e.Handled = result.Changed;
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
        else if (Workspace.ToolController.ActiveTool is GripEditTool activeGripEditTool &&
                 e.Key == Key.Delete)
        {
            result = activeGripEditTool.DeleteCurrentVertex(Workspace.Context);
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

    private ToolResult ApplyZoomWindowIfCompleted(ToolResult toolResult)
    {
        if (Workspace?.ToolController.ActiveTool is not ZoomWindowTool zoomWindowTool ||
            zoomWindowTool.CompletedWindow is not BoundingBox2D window)
        {
            return toolResult;
        }

        zoomWindowTool.ClearCompletedWindow();

        return ZoomToWindow(window);
    }

    public ToolResult ZoomToWindow(BoundingBox2D window)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return ToolResult.None("Cannot zoom window because the canvas has no size yet.");
        }

        double minimumModelSize = _viewport.ScreenLengthToModel(4);

        if (window.Width < minimumModelSize ||
            window.Height < minimumModelSize)
        {
            return ToolResult.None("Zoom window ignored because the selected window is too small.");
        }

        _viewport.ZoomToFit(
            window,
            new Size(Bounds.Width, Bounds.Height),
            screenPadding: 0);

        InvalidateVisual();

        return ToolResult.Updated("Zoom window applied.");
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
