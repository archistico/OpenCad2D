using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Defines default properties used when tools create new entities.
/// </summary>
public sealed class ToolCreationContext
{
    public ToolCreationContext(LayerId currentLayerId)
    {
        CurrentLayerId = currentLayerId;
    }

    public LayerId CurrentLayerId { get; set; }
}