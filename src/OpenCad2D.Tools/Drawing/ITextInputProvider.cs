using System.Threading.Tasks;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Provides UI-neutral single-line text input for the text tool.
/// </summary>
public interface ITextInputProvider
{
    TextInputResult? RequestText(TextInputRequest request);

    /// <summary>
    /// Asynchronously requests single-line text input. UI implementations should override this
    /// method and await their dialog instead of blocking the UI thread.
    /// </summary>
    Task<TextInputResult?> RequestTextAsync(TextInputRequest request)
    {
        return Task.FromResult(RequestText(request));
    }
}
