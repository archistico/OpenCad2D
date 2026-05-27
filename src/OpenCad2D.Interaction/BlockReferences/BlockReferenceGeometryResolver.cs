using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Interaction.BlockReferences;

/// <summary>
/// Resolves the drawable geometry contained by block references into world-space entities.
/// </summary>
internal static class BlockReferenceGeometryResolver
{
    public static IEnumerable<CadEntity> GetWorldEntities(
        CadDocument document,
        BlockReferenceEntity blockReference)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(blockReference);

        return GetWorldEntities(
            document,
            blockReference,
            new HashSet<BlockDefinitionId>());
    }

    private static IEnumerable<CadEntity> GetWorldEntities(
        CadDocument document,
        BlockReferenceEntity blockReference,
        HashSet<BlockDefinitionId> visitedDefinitionIds)
    {
        if (!visitedDefinitionIds.Add(blockReference.BlockDefinitionId))
        {
            yield break;
        }

        if (!document.BlockDefinitions.TryGet(blockReference.BlockDefinitionId, out var definition) || definition is null)
        {
            visitedDefinitionIds.Remove(blockReference.BlockDefinitionId);
            yield break;
        }

        foreach (CadEntity localEntity in definition.Entities)
        {
            CadEntity worldEntity = blockReference.TransformContainedEntity(localEntity);

            if (worldEntity is BlockReferenceEntity nestedBlockReference)
            {
                foreach (CadEntity nestedEntity in GetWorldEntities(
                    document,
                    nestedBlockReference,
                    visitedDefinitionIds))
                {
                    yield return nestedEntity;
                }

                continue;
            }

            yield return worldEntity;
        }

        visitedDefinitionIds.Remove(blockReference.BlockDefinitionId);
    }
}
