namespace OpenCad2D.Core.Identifiers;

/// <summary>
/// Strongly typed identifier for a CAD entity.
/// </summary>
public readonly record struct EntityId(Guid Value)
{
    public static EntityId New()
    {
        return new EntityId(Guid.NewGuid());
    }

    public static EntityId Empty => new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }
}