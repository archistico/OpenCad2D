using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Dimensions.Rendering;

/// <summary>
/// Geometry primitives produced from a dimension entity and a dimension style.
/// The model is renderer-agnostic and can be reused by the canvas and exporters.
/// </summary>
public sealed class DimensionRenderModel
{
    public DimensionRenderModel(
        IEnumerable<DimensionLinePrimitive> lines,
        IEnumerable<DimensionLinePrimitive> arrows,
        DimensionTextPrimitive text,
        BoundingBox2D bounds)
        : this(
            lines,
            Enumerable.Empty<DimensionArcPrimitive>(),
            arrows,
            text,
            bounds)
    {
    }

    public DimensionRenderModel(
        IEnumerable<DimensionLinePrimitive> lines,
        IEnumerable<DimensionArcPrimitive> arcs,
        IEnumerable<DimensionLinePrimitive> arrows,
        DimensionTextPrimitive text,
        BoundingBox2D bounds)
    {
        Lines = lines?.ToList() ?? throw new ArgumentNullException(nameof(lines));
        Arcs = arcs?.ToList() ?? throw new ArgumentNullException(nameof(arcs));
        Arrows = arrows?.ToList() ?? throw new ArgumentNullException(nameof(arrows));
        Text = text;
        Bounds = bounds;
    }

    public IReadOnlyList<DimensionLinePrimitive> Lines { get; }

    public IReadOnlyList<DimensionArcPrimitive> Arcs { get; }

    public IReadOnlyList<DimensionLinePrimitive> Arrows { get; }

    public DimensionTextPrimitive Text { get; }

    public BoundingBox2D Bounds { get; }
}
