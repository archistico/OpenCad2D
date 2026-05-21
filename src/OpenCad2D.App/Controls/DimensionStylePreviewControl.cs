using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using OpenCad2D.App.ViewModels.DimensionStyles;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Dimensions.Rendering;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace OpenCad2D.App.Controls;

public sealed class DimensionStylePreviewControl : Control
{
    private static readonly IBrush BackgroundBrush =
        new SolidColorBrush(Color.FromRgb(32, 32, 32));

    private static readonly IBrush TextBrush =
        new SolidColorBrush(Color.FromRgb(238, 238, 238));

    private static readonly IBrush HelpTextBrush =
        new SolidColorBrush(Color.FromRgb(170, 170, 170));

    private static readonly IBrush HighlightBrush =
        new SolidColorBrush(Color.FromRgb(255, 230, 80));

    private static readonly IBrush ConstructionBrush =
        new SolidColorBrush(Color.FromRgb(92, 92, 92));

    private static readonly Pen BorderPen =
        new(new SolidColorBrush(Color.FromRgb(58, 58, 58)), 1);

    private static readonly Pen DimensionPen =
        new(HighlightBrush, 1.4);

    private static readonly Pen ConstructionPen =
        new(ConstructionBrush, 1);

    public static readonly StyledProperty<EditableDimensionStyleViewModel?> DimensionStyleProperty =
        AvaloniaProperty.Register<DimensionStylePreviewControl, EditableDimensionStyleViewModel?>(nameof(DimensionStyle));

    private readonly DimensionGeometryBuilder _geometryBuilder = new();
    private EditableDimensionStyleViewModel? _observedStyle;

    public EditableDimensionStyleViewModel? DimensionStyle
    {
        get => GetValue(DimensionStyleProperty);
        set => SetValue(DimensionStyleProperty, value);
    }

    public DimensionStylePreviewControl()
    {
        MinHeight = 150;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DimensionStyleProperty)
        {
            SetObservedStyle(DimensionStyle);
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect bounds = Bounds;
        context.FillRectangle(BackgroundBrush, bounds);
        context.DrawRectangle(null, BorderPen, bounds.Deflate(0.5));

        EditableDimensionStyleViewModel? editableStyle = DimensionStyle;
        if (editableStyle is null)
        {
            DrawCenteredText(
                context,
                "Select a dimension style to preview it.",
                new Point(bounds.Width / 2.0, bounds.Height / 2.0),
                0,
                12,
                HelpTextBrush);
            return;
        }

        if (!TryBuildPreview(
                editableStyle,
                out DimensionStyle? style,
                out LinearDimensionEntity? dimension,
                out DimensionRenderModel? model) ||
            style is null ||
            dimension is null ||
            model is null)
        {
            DrawCenteredText(
                context,
                "Preview unavailable: fix invalid style values.",
                new Point(bounds.Width / 2.0, bounds.Height / 2.0),
                0,
                12,
                HelpTextBrush);
            return;
        }

        double width = Math.Max(bounds.Width, 260.0);
        double height = Math.Max(bounds.Height, 140.0);

        Rect drawingArea = new(
            24.0,
            30.0,
            Math.Max(1.0, width - 48.0),
            Math.Max(1.0, height - 58.0));

        PreviewTransform transform = PreviewTransform.Create(
            model.Bounds,
            drawingArea);

        DrawMeasuredReferenceLine(
            context,
            transform,
            dimension.FirstPoint,
            dimension.SecondPoint);

        foreach (DimensionLinePrimitive line in model.Lines)
        {
            context.DrawLine(
                DimensionPen,
                transform.ToScreen(line.Start),
                transform.ToScreen(line.End));
        }

        foreach (DimensionLinePrimitive arrow in model.Arrows)
        {
            context.DrawLine(
                DimensionPen,
                transform.ToScreen(arrow.Start),
                transform.ToScreen(arrow.End));
        }

        DrawDimensionText(
            context,
            model.Text,
            style,
            editableStyle,
            transform);

        DrawCenteredText(
            context,
            "Dimension style preview",
            new Point(width / 2.0, 18.0),
            0,
            11,
            HelpTextBrush);

        DrawCenteredText(
            context,
            "Uses the same DimensionGeometryBuilder as real dimensions.",
            new Point(width / 2.0, height - 11.0),
            0,
            10,
            HelpTextBrush);
    }

    private bool TryBuildPreview(
        EditableDimensionStyleViewModel editableStyle,
        out DimensionStyle? style,
        out LinearDimensionEntity? dimension,
        out DimensionRenderModel? model)
    {
        style = null;
        dimension = null;
        model = null;

        try
        {
            style = editableStyle.ToDimensionStyle();

            dimension = new LinearDimensionEntity(
                new Point2D(0, 0),
                new Point2D(100, 0),
                new Point2D(0, -style.DimensionLineOffset),
                DimensionOrientation.Horizontal,
                style.Id);

            model = _geometryBuilder.Build(
                dimension,
                style);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetObservedStyle(EditableDimensionStyleViewModel? style)
    {
        if (ReferenceEquals(_observedStyle, style))
        {
            return;
        }

        if (_observedStyle is not null)
        {
            _observedStyle.PropertyChanged -= OnObservedStylePropertyChanged;
        }

        _observedStyle = style;

        if (_observedStyle is not null)
        {
            _observedStyle.PropertyChanged += OnObservedStylePropertyChanged;
        }
    }

    private void OnObservedStylePropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private static void DrawMeasuredReferenceLine(
        DrawingContext context,
        PreviewTransform transform,
        Point2D first,
        Point2D second)
    {
        Point start = transform.ToScreen(first);
        Point end = transform.ToScreen(second);

        context.DrawLine(
            ConstructionPen,
            start,
            end);

        context.DrawEllipse(
            null,
            ConstructionPen,
            start,
            2.0,
            2.0);

        context.DrawEllipse(
            null,
            ConstructionPen,
            end,
            2.0,
            2.0);
    }

    private static void DrawDimensionText(
        DrawingContext context,
        DimensionTextPrimitive text,
        DimensionStyle style,
        EditableDimensionStyleViewModel editableStyle,
        PreviewTransform transform)
    {
        Point insertionPoint = transform.ToScreen(text.Position);
        double textHeight = TryGetSelectedTextHeight(editableStyle, out double parsedHeight)
            ? parsedHeight
            : Math.Max(2.5, style.ArrowSize * 0.75);
        double fontSize = Math.Clamp(
            textHeight * transform.Scale,
            8.0,
            18.0);

        var formattedText = new FormattedText(
            text.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            fontSize,
            TextBrush);

        Point centeredInsertionPoint = new(
            insertionPoint.X - formattedText.Width / 2.0,
            insertionPoint.Y - formattedText.Height / 2.0);

        using (context.PushTransform(CadTextTransform.CreateCadRotationAt(
                   text.RotationDegrees,
                   insertionPoint.X,
                   insertionPoint.Y)))
        {
            context.DrawText(
                formattedText,
                centeredInsertionPoint);
        }
    }

    private static bool TryGetSelectedTextHeight(
        EditableDimensionStyleViewModel editableStyle,
        out double height)
    {
        height = 0.0;

        string? displayText = editableStyle.SelectedTextFormat?.DisplayText;
        if (string.IsNullOrWhiteSpace(displayText))
        {
            return false;
        }

        string lastPart = displayText
            .Split('—')
            .LastOrDefault()?
            .Trim() ?? string.Empty;

        return double.TryParse(
            lastPart,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out height) &&
            height > 0;
    }

    private static void DrawCenteredText(
        DrawingContext context,
        string text,
        Point center,
        double rotationDegrees,
        double fontSize,
        IBrush brush)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            fontSize,
            brush);

        Point origin = new(
            center.X - formattedText.Width / 2.0,
            center.Y - formattedText.Height / 2.0);

        using (context.PushTransform(CadTextTransform.CreateCadRotationAt(
                   rotationDegrees,
                   center.X,
                   center.Y)))
        {
            context.DrawText(formattedText, origin);
        }
    }

    private readonly record struct PreviewTransform(
        double Scale,
        Vector Offset)
    {
        public static PreviewTransform Create(
            BoundingBox2D modelBounds,
            Rect drawingArea)
        {
            BoundingBox2D expanded = modelBounds.Expand(6.0);

            double modelWidth = Math.Max(expanded.Width, 1.0);
            double modelHeight = Math.Max(expanded.Height, 1.0);

            double scale = Math.Min(
                drawingArea.Width / modelWidth,
                drawingArea.Height / modelHeight);

            scale = Math.Clamp(scale, 0.25, 4.0);

            Point modelCenter = new(
                expanded.Center.X * scale,
                expanded.Center.Y * scale);

            Point screenCenter = new(
                drawingArea.X + drawingArea.Width / 2.0,
                drawingArea.Y + drawingArea.Height / 2.0);

            return new PreviewTransform(
                scale,
                new Vector(
                    screenCenter.X - modelCenter.X,
                    screenCenter.Y - modelCenter.Y));
        }

        public Point ToScreen(Point2D point)
        {
            return new Point(
                point.X * Scale + Offset.X,
                point.Y * Scale + Offset.Y);
        }
    }
}
