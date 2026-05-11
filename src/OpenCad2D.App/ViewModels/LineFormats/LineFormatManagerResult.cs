using OpenCad2D.Core.Styling;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.LineFormats;

public sealed class LineFormatManagerResult
{
    public LineFormatManagerResult(IEnumerable<LineFormat> lineFormats)
    {
        LineFormats = lineFormats.ToList();
    }

    public IReadOnlyList<LineFormat> LineFormats { get; }
}
