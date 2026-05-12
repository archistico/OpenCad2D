using System;
using Avalonia;

namespace OpenCad2D.App.Controls;

/// <summary>
/// Creates text rendering transforms for the CAD canvas.
/// </summary>
public static class CadTextTransform
{
    /// <summary>
    /// Creates a screen-space rotation matrix for CAD text.
    /// </summary>
    /// <param name="rotationDegrees">
    /// CAD rotation in degrees. Positive values must appear clockwise on screen
    /// because the canvas Y axis grows downward.
    /// </param>
    /// <param name="centerX">Rotation center X coordinate in screen space.</param>
    /// <param name="centerY">Rotation center Y coordinate in screen space.</param>
    /// <returns>A matrix that rotates around the specified screen-space center.</returns>
    public static Matrix CreateCadRotationAt(
        double rotationDegrees,
        double centerX,
        double centerY)
    {
        double radians = rotationDegrees * Math.PI / 180.0;
        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);

        double offsetX = centerX - (centerX * cosine) + (centerY * sine);
        double offsetY = centerY - (centerX * sine) - (centerY * cosine);

        return new Matrix(
            cosine,
            sine,
            -sine,
            cosine,
            offsetX,
            offsetY);
    }
}
