using OpenCad2D.Tools.Drawing;
using System;
using System.Threading.Tasks;

namespace OpenCad2D.App;

/// <summary>
/// Avalonia implementation of the text input provider used by the text tool.
/// </summary>
public sealed class AvaloniaTextInputProvider : ITextInputProvider
{
    private readonly MainWindow _owner;

    public AvaloniaTextInputProvider(MainWindow owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public TextInputResult? RequestText(TextInputRequest request)
    {
        throw new InvalidOperationException(
            "Avalonia text input must be requested asynchronously to avoid blocking the UI thread.");
    }

    public async Task<TextInputResult?> RequestTextAsync(TextInputRequest request)
    {
        var window = new TextInputWindow(request);

        return await window
            .ShowDialog<TextInputResult?>(_owner)
            .ConfigureAwait(true);
    }
}
