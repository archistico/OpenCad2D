using OpenCad2D.Core.Documents;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Represents a single undoable command composed of multiple child commands.
/// </summary>
public sealed class CompositeCommand : ICadCommand
{
    private readonly IReadOnlyList<ICadCommand> _commands;

    public CompositeCommand(
        string name,
        IEnumerable<ICadCommand> commands)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Command name cannot be empty.",
                nameof(name));
        }

        ArgumentNullException.ThrowIfNull(commands);

        _commands = commands.ToList();

        if (_commands.Count == 0)
        {
            throw new ArgumentException(
                "A composite command must contain at least one child command.",
                nameof(commands));
        }

        Name = name;
    }

    public string Name { get; }

    public IReadOnlyList<ICadCommand> Commands => _commands;

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var executedCommands = new List<ICadCommand>();

        try
        {
            foreach (ICadCommand command in _commands)
            {
                command.Execute(document);
                executedCommands.Add(command);
            }
        }
        catch
        {
            RollbackExecutedCommands(document, executedCommands);
            throw;
        }
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo(document);
        }
    }

    private static void RollbackExecutedCommands(
        CadDocument document,
        IReadOnlyList<ICadCommand> executedCommands)
    {
        for (int i = executedCommands.Count - 1; i >= 0; i--)
        {
            executedCommands[i].Undo(document);
        }
    }
}