namespace OpenCad2D.Tools.Input;

/// <summary>
/// Represents a keyword option exposed by a command prompt, for example Close/C or Undo/U.
/// </summary>
public sealed class CommandOption
{
    public CommandOption(
        string keyword,
        string shortcut,
        string description)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ArgumentException("Command option keyword cannot be empty.", nameof(keyword));
        }

        if (string.IsNullOrWhiteSpace(shortcut))
        {
            throw new ArgumentException("Command option shortcut cannot be empty.", nameof(shortcut));
        }

        Keyword = keyword.Trim();
        Shortcut = shortcut.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public string Keyword { get; }

    public string Shortcut { get; }

    public string Description { get; }

    public bool Matches(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string normalizedInput = input.Trim();

        return string.Equals(Keyword, normalizedInput, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Shortcut, normalizedInput, StringComparison.OrdinalIgnoreCase);
    }
}
