using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Identifiers;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.DimensionStyles;

public sealed class DimensionStyleManagerResult
{
    public DimensionStyleManagerResult(
        IEnumerable<DimensionStyle> dimensionStyles,
        DimensionStyleId currentDimensionStyleId)
    {
        DimensionStyles = dimensionStyles.ToList();
        CurrentDimensionStyleId = currentDimensionStyleId;
    }

    public IReadOnlyList<DimensionStyle> DimensionStyles { get; }

    public DimensionStyleId CurrentDimensionStyleId { get; }
}
