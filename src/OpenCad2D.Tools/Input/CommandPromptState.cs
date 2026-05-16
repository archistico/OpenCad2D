namespace OpenCad2D.Tools.Input;

/// <summary>
/// Describes the user-facing prompt and the input kind expected by the active CAD command.
/// </summary>
public sealed class CommandPromptState
{
    public CommandPromptState(
        string? commandName,
        string prompt,
        CommandInputKind expectedInput,
        IEnumerable<CommandOption>? options = null,
        bool acceptsEmptyEnter = false,
        string? placeholder = null)
    {
        CommandName = string.IsNullOrWhiteSpace(commandName)
            ? null
            : commandName.Trim();

        Prompt = string.IsNullOrWhiteSpace(prompt)
            ? "Command:"
            : prompt.Trim();

        ExpectedInput = expectedInput;
        Options = (options ?? Enumerable.Empty<CommandOption>()).ToArray();
        AcceptsEmptyEnter = acceptsEmptyEnter;
        Placeholder = string.IsNullOrWhiteSpace(placeholder)
            ? null
            : placeholder.Trim();
    }

    public string? CommandName { get; }

    public string Prompt { get; }

    public CommandInputKind ExpectedInput { get; }

    public IReadOnlyList<CommandOption> Options { get; }

    public bool AcceptsEmptyEnter { get; }

    public string? Placeholder { get; }

    public static CommandPromptState Idle { get; } = new(
        null,
        "Command:",
        CommandInputKind.CommandName,
        acceptsEmptyEnter: true,
        placeholder: "Command name or alias");

    public string FormatPrompt()
    {
        string optionText = Options.Count == 0
            ? string.Empty
            : $" or [{string.Join('/', Options.Select(option => option.Keyword))}]";

        string prompt = Prompt.EndsWith(':')
            ? Prompt[..^1]
            : Prompt;

        return CommandName is null
            ? $"{prompt}{optionText}:"
            : $"{CommandName}: {prompt}{optionText}:";
    }
}
