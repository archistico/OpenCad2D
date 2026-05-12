namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Safe fallback text input provider used in tests and non-UI hosts.
/// </summary>
public sealed class DefaultTextInputProvider : ITextInputProvider
{
    public TextInputResult? RequestText(TextInputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new TextInputResult(
            "Text",
            request.DefaultTextFormatId,
            request.DefaultRotationDegrees);
    }
}
