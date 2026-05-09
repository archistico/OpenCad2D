using Avalonia;
using OpenCad2D.Geometry.Primitives;
using System;

namespace OpenCad2D.App.Viewport;

/// <summary>
/// Converts points between model coordinates and screen coordinates.
/// </summary>
public sealed class ViewportTransform
{
    private const double MinScale = 0.05;
    private const double MaxScale = 100.0;

    public double Scale { get; private set; } = 1.0;

    public Vector Offset { get; private set; } = new(0, 0);

    public Point ModelToScreen(Point2D modelPoint)
    {
        return new Point(
            modelPoint.X * Scale + Offset.X,
            modelPoint.Y * Scale + Offset.Y);
    }

    public Point2D ScreenToModel(Point screenPoint)
    {
        return new Point2D(
            (screenPoint.X - Offset.X) / Scale,
            (screenPoint.Y - Offset.Y) / Scale);
    }

    public double ModelLengthToScreen(double modelLength)
    {
        return modelLength * Scale;
    }

    public double ScreenLengthToModel(double screenLength)
    {
        return screenLength / Scale;
    }

    public void Pan(Vector screenDelta)
    {
        Offset += screenDelta;
    }

    public void ZoomAt(
        Point screenPoint,
        double zoomFactor)
    {
        if (zoomFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(zoomFactor),
                "Zoom factor must be greater than zero.");
        }

        Point2D modelPointBeforeZoom = ScreenToModel(screenPoint);

        double newScale = Math.Clamp(
            Scale * zoomFactor,
            MinScale,
            MaxScale);

        Scale = newScale;

        Point screenPointAfterZoom = ModelToScreen(modelPointBeforeZoom);

        Offset += screenPoint - screenPointAfterZoom;
    }

    public void ZoomToFit(
        BoundingBox2D modelBounds,
        Size viewportSize,
        double screenPadding = 40)
    {
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0)
        {
            return;
        }

        if (screenPadding < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(screenPadding),
                "Screen padding cannot be negative.");
        }

        const double minimumModelSize = 1e-9;

        double availableWidth = Math.Max(
            1,
            viewportSize.Width - screenPadding * 2);

        double availableHeight = Math.Max(
            1,
            viewportSize.Height - screenPadding * 2);

        double boundsWidth = modelBounds.Width;
        double boundsHeight = modelBounds.Height;

        double scaleX = boundsWidth <= minimumModelSize
            ? MaxScale
            : availableWidth / boundsWidth;

        double scaleY = boundsHeight <= minimumModelSize
            ? MaxScale
            : availableHeight / boundsHeight;

        Scale = Math.Clamp(
            Math.Min(scaleX, scaleY),
            MinScale,
            MaxScale);

        Point2D modelCenter = modelBounds.Center;
        var screenCenter = new Point(
            viewportSize.Width / 2.0,
            viewportSize.Height / 2.0);

        Offset = new Vector(
            screenCenter.X - modelCenter.X * Scale,
            screenCenter.Y - modelCenter.Y * Scale);
    }

    public void Reset()
    {
        Scale = 1.0;
        Offset = new Vector(0, 0);
    }
}