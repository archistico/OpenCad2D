using OpenCad2D.Core.Documents;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Manages executed commands and provides undo/redo support.
/// </summary>
public sealed class CommandHistory
{
    private readonly Stack<ICadCommand> _undoStack = new();
    private readonly Stack<ICadCommand> _redoStack = new();

    public int UndoCount => _undoStack.Count;

    public int RedoCount => _redoStack.Count;

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void Execute(CadDocument document, ICadCommand command)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(command);

        command.Execute(document);

        _undoStack.Push(command);
        _redoStack.Clear();
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!CanUndo)
        {
            throw new InvalidOperationException("There are no commands to undo.");
        }

        ICadCommand command = _undoStack.Pop();

        command.Undo(document);

        _redoStack.Push(command);
    }

    public void Redo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!CanRedo)
        {
            throw new InvalidOperationException("There are no commands to redo.");
        }

        ICadCommand command = _redoStack.Pop();

        command.Execute(document);

        _undoStack.Push(command);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}