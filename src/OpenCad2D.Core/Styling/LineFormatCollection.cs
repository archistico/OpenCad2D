using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Styling;

/// <summary>
/// Ordered collection of line formats stored in a CAD document.
/// </summary>
public sealed class LineFormatCollection
{
    private readonly List<LineFormat> _formats;

    public LineFormatCollection()
        : this(CreateDefaultFormats())
    {
    }

    public LineFormatCollection(IEnumerable<LineFormat> formats)
    {
        ArgumentNullException.ThrowIfNull(formats);

        _formats = formats.ToList();

        Validate(_formats);
    }

    public IReadOnlyList<LineFormat> All => _formats;

    public int Count => _formats.Count;

    public static LineFormatCollection Default => new(CreateDefaultFormats());

    public LineFormat GetById(LineFormatId id)
    {
        LineFormat? format = _formats.FirstOrDefault(item => item.Id == id);

        if (format is null)
        {
            throw new KeyNotFoundException(
                $"Line format '{id}' was not found.");
        }

        return format;
    }

    public bool TryGetById(
        LineFormatId id,
        out LineFormat? format)
    {
        format = _formats.FirstOrDefault(item => item.Id == id);

        return format is not null;
    }

    public bool Contains(LineFormatId id)
    {
        return _formats.Any(format => format.Id == id);
    }

    public LineFormatCollection WithFormats(IEnumerable<LineFormat> formats)
    {
        return new LineFormatCollection(formats);
    }

    public void ReplaceAll(IEnumerable<LineFormat> formats)
    {
        ArgumentNullException.ThrowIfNull(formats);

        List<LineFormat> formatList = formats.ToList();

        Validate(formatList);

        _formats.Clear();
        _formats.AddRange(formatList);
    }

    private static void Validate(IReadOnlyList<LineFormat> formats)
    {
        if (formats.Count == 0)
        {
            throw new InvalidOperationException(
                "A CAD document must contain at least one line format.");
        }

        var duplicateId = formats
            .GroupBy(format => format.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate line format id '{duplicateId.Key}'.");
        }

        var duplicateName = formats
            .GroupBy(format => format.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate line format name '{duplicateName.Key}'.");
        }
    }

    private static IEnumerable<LineFormat> CreateDefaultFormats()
    {
        yield return new LineFormat(
            LineFormatId.Continuous,
            "Continua",
            CadColor.FromRgb(255, 255, 255),
            LineWeight.FromMillimeters(1.0),
            LineStyle.Continuous);

        yield return new LineFormat(
            LineFormatId.Axis,
            "Asse",
            CadColor.FromRgb(255, 0, 0),
            LineWeight.FromMillimeters(0.5),
            LineStyle.DashDot);

        yield return new LineFormat(
            LineFormatId.Dashed,
            "Tratteggiata",
            CadColor.FromRgb(255, 255, 0),
            LineWeight.FromMillimeters(1.0),
            LineStyle.Dashed);

        yield return new LineFormat(
            LineFormatId.DashDotDot,
            "Tratto due punti",
            CadColor.FromRgb(0, 200, 255),
            LineWeight.FromMillimeters(0.5),
            LineStyle.DashDotDot);

        yield return new LineFormat(
            LineFormatId.DashDot,
            "Tratto e punto",
            CadColor.FromRgb(0, 255, 0),
            LineWeight.FromMillimeters(0.75),
            LineStyle.DashDot);
    }
}
