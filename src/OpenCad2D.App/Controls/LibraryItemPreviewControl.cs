using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OpenCad2D.App.Rendering;
using OpenCad2D.App.ViewModels.Library;
using OpenCad2D.App.Viewport;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Tools.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenCad2D.App.Controls;

public sealed class LibraryItemPreviewControl : Control
{
    private static readonly IBrush BackgroundBrush =
        new SolidColorBrush(Color.FromRgb(37, 37, 38));

    private static readonly IBrush EmptyTextBrush =
        new SolidColorBrush(Color.FromRgb(170, 170, 170));

    private static readonly Pen BorderPen =
        new(new SolidColorBrush(Color.FromRgb(58, 58, 58)), 1);

    private static readonly Pen OriginAxisXPen =
        new(new SolidColorBrush(Color.FromArgb(150, 255, 120, 120)), 1);

    private static readonly Pen OriginAxisYPen =
        new(new SolidColorBrush(Color.FromArgb(150, 120, 255, 120)), 1);

    private readonly IDocumentSerializer _documentSerializer = new JsonDocumentSerializer();
    private readonly ViewportTransform _viewport = new();
    private readonly CadEntityRenderer _renderer;

    private LibraryCatalogItem? _loadedItem;
    private CadWorkspace? _previewWorkspace;
    private string? _loadError;

    public static readonly StyledProperty<LibraryCatalogItem?> ItemProperty =
        AvaloniaProperty.Register<LibraryItemPreviewControl, LibraryCatalogItem?>(nameof(Item));

    public LibraryCatalogItem? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public LibraryItemPreviewControl()
    {
        MinHeight = 180;
        ClipToBounds = true;
        _renderer = new CadEntityRenderer(_viewport);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemProperty)
        {
            _loadedItem = null;
            _previewWorkspace = null;
            _loadError = null;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect bounds = new(
            0,
            0,
            Bounds.Width,
            Bounds.Height);
        context.FillRectangle(BackgroundBrush, bounds);
        context.DrawRectangle(null, BorderPen, bounds.Deflate(0.5));

        LibraryCatalogItem? item = Item;
        if (item is null)
        {
            DrawCenteredText(context, "No item selected.", bounds.Center);
            return;
        }

        EnsurePreviewWorkspace(item);

        if (_loadError is not null)
        {
            DrawCenteredText(context, _loadError, bounds.Center);
            return;
        }

        if (_previewWorkspace is null)
        {
            DrawCenteredText(context, "Preview unavailable.", bounds.Center);
            return;
        }

        IReadOnlyCollection<CadEntity> entities = _previewWorkspace.Document.Entities.All;
        if (entities.Count == 0)
        {
            DrawCenteredText(context, "Empty item.", bounds.Center);
            return;
        }

        BoundingBox2D modelBounds = GetDocumentBounds(_previewWorkspace.Document);
        if (bounds.Width <= 4 || bounds.Height <= 4)
        {
            return;
        }

        _viewport.ZoomToFit(
            modelBounds,
            new Size(bounds.Width, bounds.Height),
            screenPadding: 26);

        using (context.PushClip(bounds.Deflate(1)))
        {
            DrawOriginAxes(context, modelBounds);

            foreach (CadEntity entity in entities)
            {
                if (!_previewWorkspace.Document.IsEntityVisible(entity))
                {
                    continue;
                }

                Pen pen = CreateEntityPen(
                    _previewWorkspace.Document,
                    entity);
                IBrush? fillBrush = CreateEntityFillBrush(
                    _previewWorkspace.Document,
                    entity);

                _renderer.DrawEntity(
                    context,
                    _previewWorkspace,
                    entity,
                    pen,
                    isSelected: false,
                    fillBrush);
            }
        }
    }

    private void EnsurePreviewWorkspace(LibraryCatalogItem item)
    {
        if (ReferenceEquals(_loadedItem, item))
        {
            return;
        }

        _loadedItem = item;
        _previewWorkspace = null;
        _loadError = null;

        try
        {
            DocumentRecoveryResult recovery = _documentSerializer.DeserializeWithRecovery(item.Document);
            _previewWorkspace = new CadWorkspace(
                recovery.Document,
                currentLayerId: new OpenCad2D.Core.Identifiers.LayerId(recovery.CurrentLayerId));
        }
        catch (Exception exception) when (exception is DocumentLoadException or UnsupportedDocumentVersionException or InvalidOperationException or ArgumentException)
        {
            _loadError = "Preview unavailable.";
        }
    }

    private void DrawOriginAxes(
        DrawingContext context,
        BoundingBox2D modelBounds)
    {
        double axisLength = Math.Max(
            Math.Max(modelBounds.Width, modelBounds.Height) * 0.18,
            1.0);

        Point origin = _viewport.ModelToScreen(Point2D.Origin);

        context.DrawLine(
            OriginAxisXPen,
            origin,
            _viewport.ModelToScreen(new Point2D(axisLength, 0)));
        context.DrawLine(
            OriginAxisYPen,
            origin,
            _viewport.ModelToScreen(new Point2D(0, axisLength)));
    }

    private Pen CreateEntityPen(
        CadDocument document,
        CadEntity entity)
    {
        EntityScreenStyle screenStyle = EntityScreenStyleResolver.Resolve(
            document,
            entity,
            isSelected: false);
        CadColor color = screenStyle.Color;

        return new Pen(
            new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B)),
            Math.Max(1.0, screenStyle.LineWeight),
            CreateDashStyle(screenStyle.DashPattern));
    }

    private IBrush? CreateEntityFillBrush(
        CadDocument document,
        CadEntity entity)
    {
        EntityScreenStyle screenStyle = EntityScreenStyleResolver.Resolve(
            document,
            entity,
            isSelected: false);

        if (!screenStyle.IsFillEnabled)
        {
            return null;
        }

        CadColor fillColor = screenStyle.FillColor;

        return new SolidColorBrush(Color.FromRgb(fillColor.R, fillColor.G, fillColor.B));
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

    private static BoundingBox2D GetDocumentBounds(CadDocument document)
    {
        IReadOnlyList<CadEntity> entities = document.Entities.All.ToList();

        if (entities.Count == 0)
        {
            return new BoundingBox2D(-1, -1, 1, 1);
        }

        BoundingBox2D first = entities[0].GetBoundingBox();
        double minX = first.MinX;
        double minY = first.MinY;
        double maxX = first.MaxX;
        double maxY = first.MaxY;

        foreach (CadEntity entity in entities.Skip(1))
        {
            BoundingBox2D box = entity.GetBoundingBox();
            minX = Math.Min(minX, box.MinX);
            minY = Math.Min(minY, box.MinY);
            maxX = Math.Max(maxX, box.MaxX);
            maxY = Math.Max(maxY, box.MaxY);
        }

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    private static void DrawCenteredText(
        DrawingContext context,
        string text,
        Point center)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            EmptyTextBrush);

        context.DrawText(
            formattedText,
            new Point(
                center.X - formattedText.Width / 2.0,
                center.Y - formattedText.Height / 2.0));
    }
}
