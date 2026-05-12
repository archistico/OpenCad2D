using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Dimensions;

/// <summary>
/// Creates non-associative diameter dimensions.
/// </summary>
public sealed class DiameterDimensionTool : RadialDimensionToolBase
{
    public override string Name => "Diameter Dimension";

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
            new DiameterDimensionEntity(
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
        var dimension = new DiameterDimensionEntity(
            center,
            pointOnCircle,
            textPoint,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(dimension));

        return ToolResult.Completed("Diameter dimension created.");
    }
}
