namespace OpenCad2D.Core.Identifiers;

/// <summary>
/// Strongly typed identifier for a reusable CAD block definition.
/// </summary>
public readonly record struct BlockDefinitionId(string Value)
{
    public static BlockDefinitionId New()
    {
        return new BlockDefinitionId(Guid.NewGuid().ToString());
    }

    public override string ToString()
    {
        return Value;
    }
}
