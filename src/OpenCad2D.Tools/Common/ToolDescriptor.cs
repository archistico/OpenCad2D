namespace OpenCad2D.Tools.Common;

/// <summary>
/// Describes a tool available in the tool registry.
/// </summary>
public sealed class ToolDescriptor
{
    public ToolDescriptor(
        ToolId id,
        string name,
        string displayName,
        string category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Tool name cannot be empty.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Tool display name cannot be empty.",
                nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException(
                "Tool category cannot be empty.",
                nameof(category));
        }

        Id = id;
        Name = name;
        DisplayName = displayName;
        Category = category;
    }

    public ToolId Id { get; }

    public string Name { get; }

    public string DisplayName { get; }

    public string Category { get; }
}