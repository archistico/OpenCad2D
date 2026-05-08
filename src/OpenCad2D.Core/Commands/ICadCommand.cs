using OpenCad2D.Core.Documents;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Represents an undoable command that modifies a CAD document.
/// </summary>
public interface ICadCommand
{
    string Name { get; }

    void Execute(CadDocument document);

    void Undo(CadDocument document);
}