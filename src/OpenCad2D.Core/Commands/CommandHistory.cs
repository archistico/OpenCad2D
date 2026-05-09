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

    /// <summary>
    /// Represents the current command position of the document.
    /// It increases when commands are executed or redone and decreases when commands are undone.
    /// CadWorkspace uses this value to determine whether the current document state differs from
    /// the last saved state.
    /// </summary>
    public int CurrentGeneration { get; private set; }

    public void Execute(CadDocument document, ICadCommand command)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(command);

        command.Execute(document);

        _undoStack.Push(command);
        _redoStack.Clear();
        CurrentGeneration++;
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
        CurrentGeneration--;
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
        CurrentGeneration++;
    }


    public void RegisterExternalChange()
    {
        _redoStack.Clear();
        CurrentGeneration++;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        CurrentGeneration = 0;
    }
}
