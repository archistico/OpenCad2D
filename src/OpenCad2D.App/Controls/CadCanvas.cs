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