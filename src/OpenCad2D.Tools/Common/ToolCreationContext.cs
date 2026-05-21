using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Defines default properties used when tools create new entities.
/// </summary>
public sealed class ToolCreationContext
{
    public ToolCreationContext(
        LayerId currentLayerId,
        DimensionStyleId currentDimensionStyleId)
    {
        CurrentLayerId = currentLayerId;
        CurrentTextFormatId = TextFormatId.Standard;
        CurrentDimensionStyleId = currentDimensionStyleId;
    }

    public LayerId CurrentLayerId { get; set; }

    public TextFormatId CurrentTextFormatId { get; set; }

    public DimensionStyleId CurrentDimensionStyleId { get; set; }
}