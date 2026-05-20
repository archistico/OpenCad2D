using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using OpenCad2D.App.Controls;
using OpenCad2D.App.Viewport;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Dimensions.Rendering;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenCad2D.App.Rendering;

/// <summary>
/// Renders CAD entities in screen space.
/// </summary>
/// <remarks>
/// This class is intentionally UI-rendering focused and keeps Avalonia drawing
/// details outside <see cref="CadCanvas"/>. Tool previews still use the canvas
/// for now and will be extracted in a later refactor step.
/// </remarks>
public sealed class CadEntityRenderer
{
    private readonly ViewportTransform _viewport;
    private readonly DimensionGeometryBuilder _dimensionGeometryBuilder = new();

    public CadEntityRenderer(ViewportTransform viewport)
    {
        _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
    }

    public void DrawEntity(
        DrawingContext context,
        CadWorkspace workspace,
        CadEntity entity,
        Pen pen,
        bool isSelected = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(pen);

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
                    workspace,
                    text,
                    pen,
                    isSelected);
                break;

            case MultilineTextEntity multilineText:
                DrawMultilineText(
                    context,
                    workspace,
                    multilineText,
                    pen,
                    isSelected);
                break;

            case LinearDimensionEntity linearDimension:
                DrawDimension(
                    context,
                    workspace,
                    linearDimension,
                    pen,
                    isSelected);
                break;

            case AlignedDimensionEntity alignedDimension:
                DrawDimension(
                    context,
                    workspace,
                    alignedDimension,
                    pen,
                    isSelected);
                break;

            case RadiusDimensionEntity radiusDimension:
                DrawDimension(
                    context,
                    workspace,
                    radiusDimension,
                    pen,
                    isSelected);
                break;

            case DiameterDimensionEntity diameterDimension:
                DrawDimension(
                    context,
                    workspace,
                    diameterDimension,
                    pen,
                    isSelected);
                break;

            case AngularDimensionEntity angularDimension:
                DrawDimension(
                    context,
                    workspace,
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

            case EllipseEntity ellipse:
                DrawEllipse(
                    context,
                    ellipse,
                    pen);
                break;

            case EllipticalArcEntity ellipticalArc:
                DrawEllipticalArc(
                    context,
                    ellipticalArc,
                    pen);
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

            case BezierSplineEntity spline:
                DrawBezierSpline(
                    context,
                    spline,
                    pen);
                break;
        }
    }

    private void DrawDimension(
        DrawingContext context,
        CadWorkspace workspace,
        DimensionEntity dimension,
        Pen pen,
        bool isSelected)
    {
        Pen dimensionPen = dimension.IsStale && !isSelected
            ? CreateStaleDimensionPen(pen)
            : pen;

        DimensionStyle style = ResolveDimensionStyle(
            workspace,
            dimension);
        DimensionRenderModel model = _dimensionGeometryBuilder.Build(
            dimension,
            style);

        foreach (DimensionLinePrimitive line in model.Lines)
        {
            context.DrawLine(
                dimensionPen,
                ToScreenPoint(line.Start),
                ToScreenPoint(line.End));
        }

        foreach (DimensionArcPrimitive arc in model.Arcs)
        {
            DrawDimensionArc(
                context,
                arc,
                dimensionPen);
        }

        foreach (DimensionLinePrimitive arrow in model.Arrows)
        {
            context.DrawLine(
                dimensionPen,
                ToScreenPoint(arrow.Start),
                ToScreenPoint(arrow.End));
        }

        DrawDimensionText(
            context,
            workspace,
            model.Text,
            style,
            dimensionPen,
            isSelected);
    }

    private static Pen CreateStaleDimensionPen(Pen sourcePen)
    {
        double thickness = sourcePen.Thickness;
        var brush = new SolidColorBrush(Color.FromRgb(255, 183, 77));

        return new Pen(
            brush,
            thickness,
            new DashStyle(new double[] { 6, 4 }, 0));
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
        CadWorkspace workspace,
        DimensionTextPrimitive text,
        DimensionStyle style,
        Pen pen,
        bool isSelected)
    {
        TextFormat format = ResolveDimensionTextFormat(
            workspace,
            style);
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

    private static DimensionStyle ResolveDimensionStyle(
        CadWorkspace workspace,
        DimensionEntity dimension)
    {
        if (workspace.Document.DimensionStyles.TryGetById(
                dimension.DimensionStyleId,
                out DimensionStyle? style) &&
            style is not null)
        {
            return style;
        }

        return DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard);
    }

    private static TextFormat ResolveDimensionTextFormat(
        CadWorkspace workspace,
        DimensionStyle style)
    {
        if (workspace.Document.TextFormats.TryGetById(
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
        CadWorkspace workspace,
        TextEntity text,
        Pen pen,
        bool isSelected)
    {
        TextFormat format = ResolveTextFormat(
            workspace,
            text.TextFormatId);
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

    private void DrawMultilineText(
        DrawingContext context,
        CadWorkspace workspace,
        MultilineTextEntity text,
        Pen pen,
        bool isSelected)
    {
        TextFormat format = ResolveTextFormat(
            workspace,
            text.TextFormatId);
        Point insertionPoint = ToScreenPoint(text.InsertionPoint);
        double fontSize = Math.Max(1.0, _viewport.ModelLengthToScreen(format.Height));
        double lineHeight = fontSize * 1.2;

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

        using (context.PushTransform(CadTextTransform.CreateCadRotationAt(
                   text.RotationDegrees,
                   insertionPoint.X,
                   insertionPoint.Y)))
        {
            IReadOnlyList<string> lines = text.Lines;

            for (int index = 0; index < lines.Count; index++)
            {
                var formattedText = new FormattedText(
                    lines[index],
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    brush);

                context.DrawText(
                    formattedText,
                    new Point(
                        insertionPoint.X,
                        insertionPoint.Y + index * lineHeight));
            }
        }
    }

    private static TextFormat ResolveTextFormat(
        CadWorkspace workspace,
        TextFormatId textFormatId)
    {
        if (workspace.Document.TextFormats.TryGetById(
                textFormatId,
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

    private void DrawEllipse(
        DrawingContext context,
        EllipseEntity ellipse,
        Pen pen)
    {
        IReadOnlyList<Point2D> points = ellipse.GetSamplePoints();

        for (int i = 0; i < points.Count; i++)
        {
            Point2D start = points[i];
            Point2D end = points[(i + 1) % points.Count];

            context.DrawLine(
                pen,
                ToScreenPoint(start),
                ToScreenPoint(end));
        }
    }

    private void DrawEllipticalArc(
        DrawingContext context,
        EllipticalArcEntity ellipticalArc,
        Pen pen)
    {
        IReadOnlyList<Point2D> points = ellipticalArc.GetSamplePoints();

        for (int i = 0; i < points.Count - 1; i++)
        {
            context.DrawLine(
                pen,
                ToScreenPoint(points[i]),
                ToScreenPoint(points[i + 1]));
        }
    }

    private void DrawBezierSpline(
        DrawingContext context,
        BezierSplineEntity spline,
        Pen pen)
    {
        IReadOnlyList<Point2D> points = spline.GetSamplePoints();

        for (int i = 0; i < points.Count - 1; i++)
        {
            context.DrawLine(
                pen,
                ToScreenPoint(points[i]),
                ToScreenPoint(points[i + 1]));
        }

        if (spline.IsClosed && points.Count > 1)
        {
            context.DrawLine(
                pen,
                ToScreenPoint(points[^1]),
                ToScreenPoint(points[0]));
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

    private Point ToScreenPoint(Point2D modelPoint)
    {
        return _viewport.ModelToScreen(modelPoint);
    }
}
