using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Layers;

/// <summary>
/// Represents a CAD layer.
/// </summary>
public sealed class Layer
{
    /// <summary>
    /// Creates a layer that references a reusable line format.
    /// </summary>
    public Layer(
        LayerId id,
        string name,
        LineFormatId lineFormatId,
        bool isVisible = true,
        bool isLocked = false,
        CadColor? fillColor = null)
        : this(
            id,
            name,
            lineFormatId,
            CadColor.FromRgb(255, 255, 255),
            LineWeight.FromMillimeters(0.25),
            isVisible,
            isLocked,
            fillColor)
    {
    }

    /// <summary>
    /// Transitional constructor kept so the rest of the application can keep compiling
    /// while rendering, persistence and UI are moved to LineFormat in later phases.
    /// </summary>
    public Layer(
        LayerId id,
        string name,
        CadColor? color = null,
        LineWeight? lineWeight = null,
        bool isVisible = true,
        bool isLocked = false,
        CadColor? fillColor = null)
        : this(
            id,
            name,
            LineFormatId.Continuous,
            color ?? CadColor.FromRgb(255, 255, 255),
            lineWeight ?? LineWeight.FromMillimeters(0.25),
            isVisible,
            isLocked,
            fillColor)
    {
    }

    private Layer(
        LayerId id,
        string name,
        LineFormatId lineFormatId,
        CadColor color,
        LineWeight lineWeight,
        bool isVisible,
        bool isLocked,
        CadColor? fillColor = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Layer name cannot be empty.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(lineFormatId.Value))
        {
            throw new ArgumentException(
                "Layer line format id cannot be empty.",
                nameof(lineFormatId));
        }

        Id = id;
        Name = name.Trim();
        LineFormatId = lineFormatId;
        Color = color;
        LineWeight = lineWeight;
        FillColor = fillColor ?? color;
        IsVisible = isVisible;
        IsLocked = isLocked;
    }

    public LayerId Id { get; }

    public string Name { get; }

    /// <summary>
    /// Gets the line format referenced by this layer.
    /// </summary>
    public LineFormatId LineFormatId { get; }

    /// <summary>
    /// Transitional appearance property kept until rendering and persistence are fully
    /// moved to LineFormat in the next implementation phases.
    /// </summary>
    public CadColor Color { get; }

    /// <summary>
    /// Transitional appearance property kept until rendering and persistence are fully
    /// moved to LineFormat in the next implementation phases.
    /// </summary>
    public LineWeight LineWeight { get; }

    /// <summary>
    /// Gets the solid fill color used by fillable entities on this layer.
    /// </summary>
    public CadColor FillColor { get; }

    public bool IsVisible { get; }

    public bool IsLocked { get; }

    public Layer WithName(string name)
    {
        return new Layer(
            Id,
            name,
            LineFormatId,
            Color,
            LineWeight,
            IsVisible,
            IsLocked,
            FillColor);
    }

    public Layer WithLineFormat(LineFormatId lineFormatId)
    {
        return new Layer(
            Id,
            Name,
            lineFormatId,
            Color,
            LineWeight,
            IsVisible,
            IsLocked,
            FillColor);
    }

    public Layer WithAppearance(
        CadColor color,
        LineWeight lineWeight)
    {
        return new Layer(
            Id,
            Name,
            LineFormatId,
            color,
            lineWeight,
            IsVisible,
            IsLocked,
            FillColor);
    }

    public Layer WithFillColor(CadColor fillColor)
    {
        return new Layer(
            Id,
            Name,
            LineFormatId,
            Color,
            LineWeight,
            IsVisible,
            IsLocked,
            fillColor);
    }

    public Layer WithVisibility(bool isVisible)
    {
        return new Layer(
            Id,
            Name,
            LineFormatId,
            Color,
            LineWeight,
            isVisible,
            IsLocked,
            FillColor);
    }

    public Layer WithLocked(bool isLocked)
    {
        return new Layer(
            Id,
            Name,
            LineFormatId,
            Color,
            LineWeight,
            IsVisible,
            isLocked,
            FillColor);
    }

    public static Layer Default => new(
        LayerId.Default,
        "0",
        LineFormatId.Continuous,
        CadColor.FromRgb(255, 255, 255),
        LineWeight.FromMillimeters(1.0),
        isVisible: true,
        isLocked: false);

    public static Layer Annotations => new(
        LayerId.Annotations,
        "Annotations",
        LineFormatId.Annotations,
        CadColor.FromRgb(160, 160, 160),
        LineWeight.FromMillimeters(0.8),
        isVisible: true,
        isLocked: false);

    public static Layer Walls => new(
        LayerId.Walls,
        "Walls",
        LineFormatId.Walls,
        CadColor.FromRgb(255, 255, 255),
        LineWeight.FromMillimeters(2.0),
        isVisible: true,
        isLocked: false);

    public static Layer Axis => new(
        LayerId.Axis,
        "Axis",
        LineFormatId.Axis,
        CadColor.FromRgb(255, 0, 0),
        LineWeight.FromMillimeters(0.75),
        isVisible: true,
        isLocked: false);

    public static Layer ConstructionLines => new(
        LayerId.ConstructionLines,
        "Construction lines",
        LineFormatId.Dashed,
        CadColor.FromRgb(255, 255, 0),
        LineWeight.FromMillimeters(0.75),
        isVisible: true,
        isLocked: false);
}
