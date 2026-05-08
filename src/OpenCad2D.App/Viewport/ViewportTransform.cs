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

    public void Reset()
    {
        Scale = 1.0;
        Offset = new Vector(0, 0);
    }
}