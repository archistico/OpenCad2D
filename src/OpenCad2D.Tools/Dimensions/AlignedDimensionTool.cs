using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Dimensions;

/// <summary>
/// Creates non-associative aligned dimensions.
/// </summary>
public sealed class AlignedDimensionTool : ThreePointDimensionToolBase
{
    public override string Name => "Aligned Dimension";

    public override IReadOnlyList<CadEntity> GetPreviewEntities()
    {
        if (FirstPoint is null || CurrentPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        if (SecondPoint is null)
        {
            return new CadEntity[]
            {
                new LineEntity(FirstPoint.Value, CurrentPoint.Value)
            };
        }

        return new CadEntity[]
        {
            new AlignedDimensionEntity(
                FirstPoint.Value,
                SecondPoint.Value,
                CurrentPoint.Value)
        };
    }

    protected override ToolResult CreateDimension(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint,
        Point2D dimensionLinePoint)
    {
        var dimension = new AlignedDimensionEntity(
            firstPoint,
            secondPoint,
            dimensionLinePoint,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(dimension));

        return ToolResult.Completed("Aligned dimension created.");
    }
}
