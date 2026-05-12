using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Dimensions;

/// <summary>
/// Creates non-associative horizontal linear dimensions.
/// </summary>
public sealed class HorizontalDimensionTool : ThreePointDimensionToolBase
{
    public override string Name => "Horizontal Dimension";

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
            new LinearDimensionEntity(
                FirstPoint.Value,
                SecondPoint.Value,
                CurrentPoint.Value,
                DimensionOrientation.Horizontal)
        };
    }


    protected override bool IsValidSecondPoint(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint,
        out string? validationMessage)
    {
        if (!base.IsValidSecondPoint(
                context,
                firstPoint,
                secondPoint,
                out validationMessage))
        {
            return false;
        }

        if (Math.Abs(firstPoint.X - secondPoint.X) <= double.Epsilon)
        {
            validationMessage = "Horizontal dimension requires two points with different X coordinates.";
            return false;
        }

        validationMessage = null;
        return true;
    }

    protected override ToolResult CreateDimension(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint,
        Point2D dimensionLinePoint)
    {
        var dimension = new LinearDimensionEntity(
            firstPoint,
            secondPoint,
            dimensionLinePoint,
            DimensionOrientation.Horizontal,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(dimension));

        return ToolResult.Completed("Horizontal dimension created.");
    }
}
