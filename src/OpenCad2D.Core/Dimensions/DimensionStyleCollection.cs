using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Dimensions;

/// <summary>
/// Ordered collection of reusable dimension styles stored in a CAD document.
/// </summary>
public sealed class DimensionStyleCollection
{
    private readonly List<DimensionStyle> _styles;

    public DimensionStyleCollection()
        : this(CreateDefaultStyles())
    {
    }

    public DimensionStyleCollection(IEnumerable<DimensionStyle> styles)
    {
        ArgumentNullException.ThrowIfNull(styles);

        _styles = styles.ToList();

        Validate(_styles);
    }

    public IReadOnlyList<DimensionStyle> All => _styles;

    public int Count => _styles.Count;

    public static DimensionStyleCollection Default => new(CreateDefaultStyles());

    public DimensionStyle GetById(DimensionStyleId id)
    {
        DimensionStyle? style = _styles.FirstOrDefault(item => item.Id == id);

        if (style is null)
        {
            throw new KeyNotFoundException(
                $"Dimension style '{id}' was not found.");
        }

        return style;
    }

    public bool TryGetById(
        DimensionStyleId id,
        out DimensionStyle? style)
    {
        style = _styles.FirstOrDefault(item => item.Id == id);

        return style is not null;
    }

    public bool Contains(DimensionStyleId id)
    {
        return _styles.Any(style => style.Id == id);
    }

    public void ReplaceAll(IEnumerable<DimensionStyle> styles)
    {
        ArgumentNullException.ThrowIfNull(styles);

        List<DimensionStyle> styleList = styles.ToList();

        Validate(styleList);

        _styles.Clear();
        _styles.AddRange(styleList);
    }

    private static void Validate(IReadOnlyList<DimensionStyle> styles)
    {
        if (styles.Count == 0)
        {
            throw new InvalidOperationException(
                "A CAD document must contain at least one dimension style.");
        }

        var duplicateId = styles
            .GroupBy(style => style.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate dimension style id '{duplicateId.Key}'.");
        }

        var duplicateName = styles
            .GroupBy(style => style.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate dimension style name '{duplicateName.Key}'.");
        }
    }

    private static IEnumerable<DimensionStyle> CreateDefaultStyles()
    {
        yield return new DimensionStyle(
            DimensionStyleId.Standard,
            "Standard",
            TextFormatId.Annotation,
            arrowSize: 4.0,
            textOffset: 2.0,
            extensionLineOffset: 1.5,
            extensionLineOvershoot: 2.0,
            decimalPlaces: 2,
            decimalSeparator: ".",
            suffix: string.Empty);
    }
}
