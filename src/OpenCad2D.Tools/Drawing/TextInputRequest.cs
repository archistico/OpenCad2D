using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Request sent by the text tool when it needs single-line text input.
/// </summary>
public sealed class TextInputRequest
{
    public TextInputRequest(
        Point2D insertionPoint,
        TextFormatId defaultTextFormatId,
        double defaultRotationDegrees,
        IReadOnlyList<TextFormat>? textFormats = null,
        bool isMultiline = false)
    {
        InsertionPoint = insertionPoint;
        DefaultTextFormatId = defaultTextFormatId;
        DefaultRotationDegrees = defaultRotationDegrees;
        TextFormats = textFormats ?? Array.Empty<TextFormat>();
        IsMultiline = isMultiline;
    }

    public Point2D InsertionPoint { get; }

    public TextFormatId DefaultTextFormatId { get; }

    public double DefaultRotationDegrees { get; }

    public IReadOnlyList<TextFormat> TextFormats { get; }

    public bool IsMultiline { get; }
}
