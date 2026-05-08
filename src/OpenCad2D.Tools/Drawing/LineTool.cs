using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw line entities.
/// </summary>
public sealed class LineTool : TwoPointToolBase
{
    public override string Name => "Line";

    public LineEntity? GetPreviewEntity()
    {
        if (!HasPreview || FirstPoint is null || CurrentPoint is null)
        {
            return null;
        }

        return new LineEntity(
            FirstPoint.Value,
            CurrentPoint.Value);
    }

    protected override ToolResult OnFirstPointSelected(
        ToolContext context,
        Point2D firstPoint)
    {
        return ToolResult.Started("Specify next point.");
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
        var line = new LineEntity(
            firstPoint,
            secondPoint,
            layerId: context.CurrentLayerId);

        context.CommandHistory.Execute(
            context.Document,
            new AddEntityCommand(line));

        return ToolResult.Completed("Line created.");
    }
}