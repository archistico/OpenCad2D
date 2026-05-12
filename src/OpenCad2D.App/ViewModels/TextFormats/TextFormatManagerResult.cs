using OpenCad2D.Core.Styling;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.TextFormats;

public sealed class TextFormatManagerResult
{
    public TextFormatManagerResult(IEnumerable<TextFormat> textFormats)
    {
        TextFormats = textFormats.ToList();
    }

    public IReadOnlyList<TextFormat> TextFormats { get; }
}
