using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Copies entities by applying a displacement and assigning new ids.
/// </summary>
public sealed class CopyEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<EntityId> _sourceEntityIds;
    private readonly Vector2D _displacement;
    private List<CadEntity>? _createdEntities;

    public CopyEntitiesCommand(
        IEnumerable<EntityId> sourceEntityIds,
        Vector2D displacement)
    {
        ArgumentNullException.ThrowIfNull(sourceEntityIds);

        _sourceEntityIds = sourceEntityIds.ToList();

        if (_sourceEntityIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one source entity id is required.",
                nameof(sourceEntityIds));
        }

        _displacement = displacement;
    }

    public string Name => "Copy entities";

    public IReadOnlyList<CadEntity> CreatedEntities
    {
        get
        {
            if (_createdEntities is null)
            {
                return Array.Empty<CadEntity>();
            }

            return _createdEntities;
        }
    }

    public void Execute(CadDocument document)
    {
        Matrix2D matrix = Matrix2D.Translation(
            _displacement.X,
            _displacement.Y);

        IReadOnlyList<CadEntity> sourceEntities =
            document.Entities.GetByIds(_sourceEntityIds);

        _createdEntities = sourceEntities
            .Select(entity => entity
                .Transform(matrix)
                .WithId(EntityId.New()))
            .ToList();

        document.AddEntities(_createdEntities);
    }

    public void Undo(CadDocument document)
    {
        if (_createdEntities is null)
        {
            throw new InvalidOperationException(
                "Cannot undo copy before execute.");
        }

        document.Entities.RemoveMany(
            _createdEntities.Select(entity => entity.Id));
    }
}