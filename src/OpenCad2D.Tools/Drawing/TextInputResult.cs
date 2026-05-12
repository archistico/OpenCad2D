using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Single-line text input returned to the text tool.
/// </summary>
public sealed class TextInputResult
{
    public TextInputResult(
        string text,
        TextFormatId textFormatId,
        double rotationDegrees)
    {
        Text = text;
        TextFormatId = textFormatId;
        RotationDegrees = rotationDegrees;
    }

    public string Text { get; }

    public TextFormatId TextFormatId { get; }

    public double RotationDegrees { get; }
}
