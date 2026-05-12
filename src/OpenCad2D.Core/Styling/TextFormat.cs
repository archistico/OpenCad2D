using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Styling;

/// <summary>
/// Defines a named, reusable single-line text format stored in a CAD document.
/// </summary>
public sealed class TextFormat
{
    public TextFormat(
        TextFormatId id,
        string name,
        string fontFamily,
        double height,
        CadColor color,
        bool isBold = false,
        bool isItalic = false)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException(
                "Text format id cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Text format name cannot be empty.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            throw new ArgumentException(
                "Text format font family cannot be empty.",
                nameof(fontFamily));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "Text format height must be greater than zero.");
        }

        Id = id;
        Name = name.Trim();
        FontFamily = fontFamily.Trim();
        Height = height;
        Color = color;
        IsBold = isBold;
        IsItalic = isItalic;
    }

    public TextFormatId Id { get; }

    public string Name { get; }

    public string FontFamily { get; }

    public double Height { get; }

    public CadColor Color { get; }

    public bool IsBold { get; }

    public bool IsItalic { get; }

    /// <summary>
    /// Gets whether the format is a built-in format that should always exist in a new document.
    /// </summary>
    public bool IsBuiltIn =>
        Id == TextFormatId.Standard ||
        Id == TextFormatId.Title ||
        Id == TextFormatId.Annotation ||
        Id == TextFormatId.Small;

    public TextFormat WithName(string name)
    {
        return new TextFormat(
            Id,
            name,
            FontFamily,
            Height,
            Color,
            IsBold,
            IsItalic);
    }

    public TextFormat WithAppearance(
        string fontFamily,
        double height,
        CadColor color,
        bool isBold,
        bool isItalic)
    {
        return new TextFormat(
            Id,
            Name,
            fontFamily,
            height,
            color,
            isBold,
            isItalic);
    }
}
