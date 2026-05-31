using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using OpenCad2D.App.Diagnostics;
using OpenCad2D.App.Rendering;
using OpenCad2D.App.Viewport;
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
using OpenCad2D.Tools.Input;
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
    private readonly IBrush _backgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private readonly ViewportTransform _viewport = new();
    private readonly CadEntityRenderer _entityRenderer;
    private readonly CadToolPreviewRenderer _toolPreviewRenderer;
    private IApplicationLogger _logger = TraceApplicationLogger.Instance;
    private Point? _pointerScreenPoint;
    private bool _isPointerInside;
    private readonly Dictionary<PenCacheKey, Pen> _penCache = new();
    private readonly Dictionary<BrushCacheKey, IBrush> _brushCache = new();
    private readonly record struct PenCacheKey(
        byte R,
        byte G,
        byte B,
        double Thickness,
        string DashPatternKey,
        double Scale);
    private readonly record struct BrushCacheKey(
        byte R,
        byte G,
        byte B);
    private const double GridLineDetectionTolerance = 1e-6;
    private bool _isPanning;
    private readonly AsyncReentrancyGuard _textInputDialogGuard = new();
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
    private SnapKind? _enabledSnapsOverride;
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

    public IApplicationLogger Logger
    {
        get => _logger;
        set => _logger = value ?? throw new ArgumentNullException(nameof(value));
    }

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

    public SnapKind? EnabledSnapsOverride
    {
        get => _enabledSnapsOverride;
        set
        {
            _enabledSnapsOverride = value;
            _currentSnapCandidate = null;
            InvalidateVisual();
        }
    }

    public CadCanvas()
    {
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.None);
        _entityRenderer = new CadEntityRenderer(_viewport);
        _toolPreviewRenderer = new CadToolPreviewRenderer(_viewport, _entityRenderer);

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
            IBrush? fillBrush = GetOrCreateEntityFillBrush(entity);

            _entityRenderer.DrawEntity(
                context,
                Workspace,
                entity,
                pen,
                isSelected,
                fillBrush);
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
            FormatDashPatternKey(screenStyle.DashPattern),
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

        DashStyle? dashStyle = CreateDashStyle(screenStyle.DashPattern);

        var pen = new Pen(
            brush,
            thickness,
            dashStyle);

        _penCache.Add(key, pen);

        return pen;
    }

    private IBrush? GetOrCreateEntityFillBrush(CadEntity entity)
    {
        if (Workspace is null)
        {
            return null;
        }

        EntityScreenStyle screenStyle = EntityScreenStyleResolver.Resolve(
            Workspace.Document,
            entity,
            isSelected: false);

        if (!screenStyle.IsFillEnabled)
        {
            return null;
        }

        CadColor fillColor = screenStyle.FillColor;
        var key = new BrushCacheKey(
            fillColor.R,
            fillColor.G,
            fillColor.B);

        if (_brushCache.TryGetValue(key, out IBrush? cachedBrush))
        {
            return cachedBrush;
        }

        var brush = new SolidColorBrush(
            Color.FromRgb(
                fillColor.R,
                fillColor.G,
                fillColor.B));

        _brushCache.Add(key, brush);

        return brush;
    }

    private DashStyle? CreateDashStyle(IReadOnlyList<double> modelPattern)
    {
        if (modelPattern.Count == 0)
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

    private static string FormatDashPatternKey(IReadOnlyList<double> values)
    {
        return values.Count == 0
            ? string.Empty
            : string.Join(";", values.Select(value => value.ToString("G17", CultureInfo.InvariantCulture)));
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

        if (Workspace.ToolController.ActiveTool is ISnapModeProvider provider &&
            Workspace.ToolController.ActiveTool is not SelectionTool)
        {
            return provider.GetActiveSnapKind(Workspace.Context);
        }

        if (_enabledSnapsOverride.HasValue)
        {
            return _enabledSnapsOverride.Value;
        }

        if (Workspace.ToolController.ActiveTool is ISnapModeProvider selectionProvider)
        {
            return selectionProvider.GetActiveSnapKind(Workspace.Context);
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
        Point2D mousePosition,
        bool isPointerPressed = false)
    {
        WorkspaceChanged?.Invoke(
            this,
            new CadCanvasWorkspaceChangedEventArgs(
                result,
                mousePosition,
                _currentSnapCandidate,
                isPointerPressed,
                _pointerScreenPoint));
    }

    private void DrawActiveToolPreview(DrawingContext context)
    {
        _toolPreviewRenderer.DrawActiveToolPreview(
            context,
            Workspace);
    }

    private async void OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        try
        {
            await OnPointerPressedAsync(e).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            HandlePointerPressedException(e, exception);
        }
    }

    private async Task OnPointerPressedAsync(PointerPressedEventArgs e)
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
            ToolResult rightClickResult = Workspace.ToolController.ConfirmActiveToolCommand();

            if (rightClickResult.Kind == ToolResultKind.None &&
                Workspace.ToolController.ActiveTool is not OpenCad2D.Tools.Input.ICommandDrivenTool)
            {
                RepeatLastCommandRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Point2D rightClickModelPoint = ToModelPoint(position);

                // Right-click can advance selection-oriented modify tools
                // (Move/Copy/Rotate/Scale) from entity selection to point input.
                // Recompute the snap candidate immediately with the new tool state;
                // otherwise the canvas can keep the previous EntityOnly marker and
                // the user does not see endpoint/midpoint/center snaps while choosing
                // the base point.
                UpdateCurrentSnapCandidate(rightClickModelPoint);

                NotifyWorkspaceChanged(
                    rightClickResult,
                    rightClickModelPoint);

                InvalidateVisual();
            }

            e.Handled = true;
            return;
        }

        Point2D modelPoint = ToModelPoint(position);
        UpdateCurrentSnapCandidate(modelPoint);

        ToolResult result;

        if (Workspace.ToolController.ActiveTool is IAsyncCadTool)
        {
            if (!_textInputDialogGuard.TryEnter(out IDisposable dialogLease))
            {
                return;
            }

            try
            {
                result = await Workspace.ToolController.OnPointerPressedAsync(
                    CreatePointerInfo(
                        position,
                        e.KeyModifiers));
            }
            finally
            {
                dialogLease.Dispose();
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
            modelPoint,
            isPointerPressed: true);

        InvalidateVisual();
    }

    private void HandlePointerPressedException(
        PointerPressedEventArgs e,
        Exception exception)
    {
        _logger.Error(
            nameof(CadCanvas),
            "Unhandled exception while processing pointer input.",
            exception);

        Point position = e.GetPosition(this);
        Point2D modelPoint = ToModelPoint(position);

        NotifyWorkspaceChanged(
            ToolResult.Cancelled("Tool input failed: " + exception.Message),
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
        else if (TryHandleActiveToolKey(e.Key, out result))
        {
            if (e.Key != Key.Delete)
            {
                ClearSnapMarker();
            }

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
        else if (e.Key == Key.Tab &&
                 Workspace.ToolController.ActiveTool is SelectionTool)
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

    private bool TryHandleActiveToolKey(
        Key key,
        out ToolResult? result)
    {
        result = null;

        CadWorkspace? workspace = Workspace;

        if (workspace?.ToolController.ActiveTool is not IKeyboardAwareTool keyboardAwareTool)
        {
            return false;
        }

        if (!TryMapKey(key, out CadToolKey toolKey))
        {
            return false;
        }

        bool handled = keyboardAwareTool.TryHandleKey(
            workspace.Context,
            toolKey,
            out ToolResult toolResult);

        if (!handled)
        {
            return false;
        }

        result = toolResult;
        return true;
    }

    private static bool TryMapKey(
        Key key,
        out CadToolKey toolKey)
    {
        switch (key)
        {
            case Key.Enter:
                toolKey = CadToolKey.Enter;
                return true;

            case Key.Delete:
                toolKey = CadToolKey.Delete;
                return true;

            case Key.C:
                toolKey = CadToolKey.C;
                return true;

            case Key.S:
                toolKey = CadToolKey.S;
                return true;

            case Key.A:
                toolKey = CadToolKey.A;
                return true;

            case Key.L:
                toolKey = CadToolKey.L;
                return true;

            case Key.U:
                toolKey = CadToolKey.U;
                return true;

            default:
                toolKey = default;
                return false;
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
