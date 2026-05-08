using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Selection;
using OpenCad2D.App.Viewport;
using System;
using System.Collections.Generic;

namespace OpenCad2D.App.Controls;

public sealed class CadCanvas : Control
{
    private readonly Pen _entityPen = new(Brushes.White, 1);
    private readonly Pen _selectedPen = new(Brushes.DeepSkyBlue, 2);
    private readonly Pen _previewPen = new(Brushes.Orange, 1);
    private readonly Pen _selectionWindowPen = new(Brushes.LightGreen, 1);
    private readonly IBrush _backgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private readonly IBrush _selectionWindowFill = new SolidColorBrush(Color.FromArgb(35, 80, 180, 255));
    private readonly ViewportTransform _viewport = new();

    private bool _isPanning;
    private Point _lastPanScreenPoint;

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

    public ViewportTransform Viewport => _viewport;

    public static readonly StyledProperty<CadWorkspace?> WorkspaceProperty =
        AvaloniaProperty.Register<CadCanvas, CadWorkspace?>(
            nameof(Workspace));

    public CadWorkspace? Workspace
    {
        get => GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    public event EventHandler<CadCanvasWorkspaceChangedEventArgs>? WorkspaceChanged;

    public CadCanvas()
    {
        Focusable = true;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        KeyDown += OnKeyDown;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(
            _backgroundBrush,
            Bounds);

        DrawGrid(context);

        if (Workspace is null)
        {
            return;
        }

        foreach (CadEntity entity in Workspace.Document.Entities.All)
        {
            DrawEntity(
                context,
                entity,
                Workspace.SelectionSet.Contains(entity.Id)
                    ? _selectedPen
                    : _entityPen);
        }

        DrawActiveToolPreview(context);
    }

    private void DrawGrid(DrawingContext context)
    {
        double minorStep = GetNiceGridStep();

        if (minorStep <= 0)
        {
            return;
        }

        double majorStep = minorStep * 5;

        Point2D topLeftModel = ToModelPoint(new Point(0, 0));
        Point2D bottomRightModel = ToModelPoint(new Point(Bounds.Width, Bounds.Height));

        double minX = Math.Min(topLeftModel.X, bottomRightModel.X);
        double maxX = Math.Max(topLeftModel.X, bottomRightModel.X);
        double minY = Math.Min(topLeftModel.Y, bottomRightModel.Y);
        double maxY = Math.Max(topLeftModel.Y, bottomRightModel.Y);

        double startX = Math.Floor(minX / minorStep) * minorStep;
        double endX = Math.Ceiling(maxX / minorStep) * minorStep;

        double startY = Math.Floor(minY / minorStep) * minorStep;
        double endY = Math.Ceiling(maxY / minorStep) * minorStep;

        for (double x = startX; x <= endX; x += minorStep)
        {
            Pen pen = IsMultipleOf(x, majorStep)
                ? _gridMajorPen
                : _gridMinorPen;

            Point p1 = ToScreenPoint(new Point2D(x, startY));
            Point p2 = ToScreenPoint(new Point2D(x, endY));

            context.DrawLine(
                pen,
                p1,
                p2);
        }

        for (double y = startY; y <= endY; y += minorStep)
        {
            Pen pen = IsMultipleOf(y, majorStep)
                ? _gridMajorPen
                : _gridMinorPen;

            Point p1 = ToScreenPoint(new Point2D(startX, y));
            Point p2 = ToScreenPoint(new Point2D(endX, y));

            context.DrawLine(
                pen,
                p1,
                p2);
        }

        DrawAxes(
            context,
            minX,
            maxX,
            minY,
            maxY);
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
        if (step <= 0)
        {
            return false;
        }

        double quotient = value / step;

        return Math.Abs(quotient - Math.Round(quotient)) < 1e-6;
    }

    private void NotifyWorkspaceChanged(
        ToolResult result,
        Point2D mousePosition)
    {
        WorkspaceChanged?.Invoke(
            this,
            new CadCanvasWorkspaceChangedEventArgs(
                result,
                mousePosition));
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

            case SelectionTool selectionTool:
                DrawSelectionPreview(context, selectionTool);
                break;
        }
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

        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _lastPanScreenPoint = position;
            e.Pointer.Capture(this);
            return;
        }

        Point2D modelPoint = ToModelPoint(position);

        ToolResult result = Workspace.ToolController.OnPointerPressed(
            new PointerInfo(
                modelPoint,
                GetModifiers(e.KeyModifiers)));

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

        if (_isPanning)
        {
            Vector delta = position - _lastPanScreenPoint;

            _viewport.Pan(delta);
            _lastPanScreenPoint = position;

            Point2D modelPoint = ToModelPoint(position);

            NotifyWorkspaceChanged(
                ToolResult.Updated("Pan."),
                modelPoint);

            InvalidateVisual();
            return;
        }

        Point2D point = ToModelPoint(position);

        ToolResult result = Workspace.ToolController.OnPointerMoved(
            new PointerInfo(
                point,
                GetModifiers(e.KeyModifiers)));

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

        Point2D modelPoint = ToModelPoint(position);

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
            new PointerInfo(
                modelPoint,
                GetModifiers(e.KeyModifiers)));

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
            result = Workspace.ActionController.CancelActiveTool();
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
        else if (e.Key == Key.Home)
        {
            _viewport.Reset();

            NotifyWorkspaceChanged(
                ToolResult.Updated("View reset."),
                Point2D.Origin);

            InvalidateVisual();
            return;
        }

        if (result is not null)
        {
            NotifyWorkspaceChanged(
                result,
                Point2D.Origin);

            InvalidateVisual();
        }
    }

    private static PointerModifiers GetModifiers(
        KeyModifiers modifiers)
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
}