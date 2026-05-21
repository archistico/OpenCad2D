using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Dimensions;

/// <summary>
/// Creates non-associative radius dimensions.
/// </summary>
public sealed class RadiusDimensionTool : RadialDimensionToolBase
{
    public override string Name => "Radius Dimension";

    public override IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        if (Center is null || CurrentPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        if (PointOnCircle is null)
        {
            return new CadEntity[]
            {
                new LineEntity(Center.Value, CurrentPoint.Value)
            };
        }

        return new CadEntity[]
        {
            new RadiusDimensionEntity(
                Center.Value,
                PointOnCircle.Value,
                CurrentPoint.Value)
        };
    }

    protected override ToolResult CreateDimension(
        ToolContext context,
        Point2D center,
        Point2D pointOnCircle,
        Point2D textPoint)
    {
        var dimension = new RadiusDimensionEntity(
            center,
            pointOnCircle,
            textPoint,
            dimensionStyleId: context.Creation.CurrentDimensionStyleId,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(dimension));

        return ToolResult.Completed("Radius dimension created.");
    }
}
