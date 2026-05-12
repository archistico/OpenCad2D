using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Associates one textual command alias with a CAD tool.
/// </summary>
public sealed class CommandAlias
{
    public CommandAlias(
        string name,
        ToolId toolId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Command alias cannot be empty.",
                nameof(name));
        }

        Name = name.Trim();
        ToolId = toolId;
    }

    public string Name { get; }

    public ToolId ToolId { get; }
}
