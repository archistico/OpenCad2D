using System;

namespace OpenCad2D.App.ViewModels.ImportDrawing;

public sealed class OpenCad2DImportPlacementOptions
{
    public OpenCad2DImportPlacementOptions(
        double scale,
        double rotationDegrees)
    {
        if (!double.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scale),
                scale,
                "Import scale must be a positive finite value.");
        }

        if (!double.IsFinite(rotationDegrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationDegrees),
                rotationDegrees,
                "Import rotation must be a finite value.");
        }

        Scale = scale;
        RotationDegrees = rotationDegrees;
    }

    public double Scale { get; }

    public double RotationDegrees { get; }

    public static OpenCad2DImportPlacementOptions Default { get; } = new(1, 0);
}
