namespace OpenCad2D.Core.Identifiers;

/// <summary>
/// Strongly typed identifier for a CAD layer.
/// </summary>
public readonly record struct LayerId(string Value)
{
    public static LayerId Default => new("0");

    public override string ToString()
    {
        return Value;
    }
}