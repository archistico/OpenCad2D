using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces the document block definition collection as a single undoable operation.
/// </summary>
public sealed class UpdateBlockDefinitionsCommand : ICadCommand
{
    private readonly IReadOnlyList<BlockDefinition> _oldDefinitions;
    private readonly IReadOnlyList<BlockDefinition> _newDefinitions;

    public UpdateBlockDefinitionsCommand(
        IEnumerable<BlockDefinition> oldDefinitions,
        IEnumerable<BlockDefinition> newDefinitions)
    {
        ArgumentNullException.ThrowIfNull(oldDefinitions);
        ArgumentNullException.ThrowIfNull(newDefinitions);

        _oldDefinitions = oldDefinitions.ToList();
        _newDefinitions = newDefinitions.ToList();
    }

    public string Name => "Update Block Definitions";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateReferencedDefinitionsExist(
            document,
            _newDefinitions);

        document.BlockDefinitions.ReplaceAll(_newDefinitions);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateReferencedDefinitionsExist(
            document,
            _oldDefinitions);

        document.BlockDefinitions.ReplaceAll(_oldDefinitions);
    }

    private static void ValidateReferencedDefinitionsExist(
        CadDocument document,
        IReadOnlyList<BlockDefinition> definitions)
    {
        HashSet<BlockDefinitionId> availableDefinitionIds = definitions
            .Select(definition => definition.Id)
            .ToHashSet();

        foreach (BlockDefinitionId definitionId in document.Entities.All
                     .OfType<BlockReferenceEntity>()
                     .Select(reference => reference.BlockDefinitionId)
                     .Distinct())
        {
            if (!availableDefinitionIds.Contains(definitionId))
            {
                throw new InvalidOperationException(
                    $"Cannot update block definitions because definition '{definitionId}' is still used by one or more block references.");
            }
        }
    }
}
