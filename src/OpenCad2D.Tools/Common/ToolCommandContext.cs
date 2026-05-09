using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides command execution services for CAD tools.
/// </summary>
public sealed class ToolCommandContext
{
    public ToolCommandContext(CommandHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        History = history;
    }

    public CommandHistory History { get; }

    public bool CanUndo => History.CanUndo;

    public bool CanRedo => History.CanRedo;

    public void Execute(
        CadDocument document,
        ICadCommand command)
    {
        History.Execute(document, command);
    }

    public void Undo(CadDocument document)
    {
        History.Undo(document);
    }

    public void Redo(CadDocument document)
    {
        History.Redo(document);
    }
}