using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw circle entities by center and radius point.
/// </summary>
public sealed class CircleTool : TwoPointToolBase
{
    public override string Name => "Circle";

    public CircleEntity? GetPreviewEntity()
    {
        if (!HasPreview || FirstPoint is null || CurrentPoint is null)
        {
            return null;
        }

        double radius = FirstPoint.Value.DistanceTo(CurrentPoint.Value);

        if (radius <= 0)
        {
            return null;
        }

        return new CircleEntity(
            FirstPoint.Value,
            radius);
    }

    protected override ToolResult OnFirstPointSelected(
        ToolContext context,
        Point2D firstPoint)
    {
        return ToolResult.Started(
            "Specify radius point or type radius.");
    }

    protected override ToolResult OnPreviewUpdated(
        ToolContext context,
        Point2D firstPoint,
        Point2D currentPoint)
    {
        return ToolResult.Updated();
    }

    protected override ToolResult OnSecondPointSelected(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint)
    {
        double radius = firstPoint.DistanceTo(secondPoint);

        if (context.GeometryTolerance.AreDistancesEqual(radius, 0))
        {
            return ToolResult.None(
                "Circle radius must be greater than zero.");
        }

        var circle = new CircleEntity(
            firstPoint,
            radius,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(circle));

        return ToolResult.Completed("Circle created.");
    }
}
