using System;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.App.Settings;

public sealed class ApplicationGridSettings
{
    public string Kind { get; set; } = GridKind.Rectangular.ToString();

    public bool IsVisible { get; set; } = true;

    public double MinorStep { get; set; } = 10;

    public double MajorStep { get; set; } = 50;

    public double OriginX { get; set; }

    public double OriginY { get; set; }

    public double MinimumScreenSpacing { get; set; } = 8;

    public double MaximumScreenSpacing { get; set; } = 220;

    public double IsometricAngleDegrees { get; set; } = 30;

    public static ApplicationGridSettings FromGridSettings(GridSettings settings)
    {
        return new ApplicationGridSettings
        {
            Kind = settings.Kind.ToString(),
            IsVisible = settings.IsVisible,
            MinorStep = settings.MinorStep,
            MajorStep = settings.MajorStep,
            OriginX = settings.OriginX,
            OriginY = settings.OriginY,
            MinimumScreenSpacing = settings.MinimumScreenSpacing,
            MaximumScreenSpacing = settings.MaximumScreenSpacing,
            IsometricAngleDegrees = settings.IsometricAngleDegrees
        };
    }

    public ApplicationGridSettings Normalize()
    {
        if (!Enum.TryParse(Kind, ignoreCase: true, out GridKind _))
        {
            Kind = GridKind.Rectangular.ToString();
        }

        if (MinorStep <= 0)
        {
            MinorStep = 10;
        }

        if (MajorStep < MinorStep)
        {
            MajorStep = Math.Max(MinorStep, 10);
        }

        if (MinimumScreenSpacing <= 0)
        {
            MinimumScreenSpacing = 8;
        }

        if (MaximumScreenSpacing <= MinimumScreenSpacing)
        {
            MaximumScreenSpacing = Math.Max(MinimumScreenSpacing + 1, 220);
        }

        if (IsometricAngleDegrees <= 0 || IsometricAngleDegrees >= 90)
        {
            IsometricAngleDegrees = 30;
        }

        return this;
    }

    public GridSettings ToGridSettings()
    {
        GridKind kind = Enum.TryParse(Kind, ignoreCase: true, out GridKind parsedKind)
            ? parsedKind
            : GridKind.Rectangular;

        try
        {
            return new GridSettings(
                step: MinorStep,
                originX: OriginX,
                originY: OriginY,
                isVisible: IsVisible,
                majorStep: MajorStep,
                minimumScreenSpacing: MinimumScreenSpacing,
                maximumScreenSpacing: MaximumScreenSpacing,
                kind: kind,
                isometricAngleDegrees: IsometricAngleDegrees);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new GridSettings();
        }
    }
}
