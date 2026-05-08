namespace OpenCad2D.Core.Styling;

/// <summary>
/// Visual style assigned to a CAD entity.
/// </summary>
public sealed record EntityStyle
{
    public CadColor Color { get; init; } = CadColor.ByLayer;

    public LineWeight LineWeight { get; init; } = LineWeight.ByLayer;

    public LineTypeId LineTypeId { get; init; } = LineTypeId.ByLayer;

    public double LineTypeScale { get; init; } = 1.0;

    public static EntityStyle ByLayer => new();
}