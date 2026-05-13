namespace OpenCad2D.Core.Identifiers;

/// <summary>
/// Strongly typed identifier for a CAD layer.
/// </summary>
public readonly record struct LayerId(string Value)
{
    public static LayerId Default => new("0");

    public static LayerId Annotations => new("Annotations");

    public static LayerId Walls => new("Walls");

    public static LayerId Axis => new("Axis");

    public static LayerId ConstructionLines => new("Construction lines");

    public override string ToString()
    {
        return Value;
    }
}