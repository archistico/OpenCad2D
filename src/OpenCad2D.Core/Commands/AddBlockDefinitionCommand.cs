using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Documents;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Adds a reusable block definition to a CAD document.
/// </summary>
public sealed class AddBlockDefinitionCommand : ICadCommand
{
    private readonly BlockDefinition _definition;

    public AddBlockDefinitionCommand(BlockDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        _definition = definition;
    }

    public string Name => "Add block definition";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.BlockDefinitions.Add(_definition);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.BlockDefinitions.RemoveRequired(_definition.Id);
    }
}
