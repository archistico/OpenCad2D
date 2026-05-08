namespace OpenCad2D.Core.Styling;

/// <summary>
/// Identifier for a CAD line type.
/// </summary>
public readonly record struct LineTypeId(string Value)
{
    public static LineTypeId ByLayer => new("ByLayer");

    public static LineTypeId Continuous => new("Continuous");

    public override string ToString()
    {
        return Value;
    }
}