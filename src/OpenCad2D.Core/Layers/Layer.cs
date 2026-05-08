using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Layers;

/// <summary>
/// Represents a CAD layer.
/// </summary>
public sealed class Layer
{
    public Layer(
        LayerId id,
        string name,
        CadColor? color = null,
        LineWeight? lineWeight = null,
        bool isVisible = true,
        bool isLocked = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Layer name cannot be empty.",
                nameof(name));
        }

        Id = id;
        Name = name;
        Color = color ?? CadColor.FromRgb(255, 255, 255);
        LineWeight = lineWeight ?? LineWeight.FromMillimeters(0.25);
        IsVisible = isVisible;
        IsLocked = isLocked;
    }

    public LayerId Id { get; }

    public string Name { get; }

    public CadColor Color { get; }

    public LineWeight LineWeight { get; }

    public bool IsVisible { get; }

    public bool IsLocked { get; }

    public static Layer Default => new(
        LayerId.Default,
        "0",
        CadColor.FromRgb(255, 255, 255),
        LineWeight.FromMillimeters(0.25));
}