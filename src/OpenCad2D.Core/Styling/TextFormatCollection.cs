using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Styling;

/// <summary>
/// Ordered collection of reusable single-line text formats stored in a CAD document.
/// </summary>
public sealed class TextFormatCollection
{
    private readonly List<TextFormat> _formats;

    public TextFormatCollection()
        : this(CreateDefaultFormats())
    {
    }

    public TextFormatCollection(IEnumerable<TextFormat> formats)
    {
        ArgumentNullException.ThrowIfNull(formats);

        _formats = formats.ToList();

        Validate(_formats);
    }

    public IReadOnlyList<TextFormat> All => _formats;

    public int Count => _formats.Count;

    public static TextFormatCollection Default => new(CreateDefaultFormats());

    public TextFormat GetById(TextFormatId id)
    {
        TextFormat? format = _formats.FirstOrDefault(item => item.Id == id);

        if (format is null)
        {
            throw new KeyNotFoundException(
                $"Text format '{id}' was not found.");
        }

        return format;
    }

    public bool TryGetById(
        TextFormatId id,
        out TextFormat? format)
    {
        format = _formats.FirstOrDefault(item => item.Id == id);

        return format is not null;
    }

    public bool Contains(TextFormatId id)
    {
        return _formats.Any(format => format.Id == id);
    }

    public void ReplaceAll(IEnumerable<TextFormat> formats)
    {
        ArgumentNullException.ThrowIfNull(formats);

        List<TextFormat> formatList = formats.ToList();

        Validate(formatList);

        _formats.Clear();
        _formats.AddRange(formatList);
    }

    private static void Validate(IReadOnlyList<TextFormat> formats)
    {
        if (formats.Count == 0)
        {
            throw new InvalidOperationException(
                "A CAD document must contain at least one text format.");
        }

        var duplicateId = formats
            .GroupBy(format => format.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate text format id '{duplicateId.Key}'.");
        }

        var duplicateName = formats
            .GroupBy(format => format.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate text format name '{duplicateName.Key}'.");
        }
    }

    private static IEnumerable<TextFormat> CreateDefaultFormats()
    {
        yield return new TextFormat(
            TextFormatId.Standard,
            "Standard",
            "Arial",
            10.0,
            CadColor.FromRgb(255, 255, 255));

        yield return new TextFormat(
            TextFormatId.Title,
            "Title",
            "Arial",
            18.0,
            CadColor.FromRgb(255, 255, 255),
            isBold: true);

        yield return new TextFormat(
            TextFormatId.Annotation,
            "Annotation",
            "Arial",
            8.0,
            CadColor.FromRgb(255, 255, 0));

        yield return new TextFormat(
            TextFormatId.Small,
            "Small",
            "Arial",
            6.0,
            CadColor.FromRgb(180, 180, 180));
    }
}
