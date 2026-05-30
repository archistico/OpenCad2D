using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Measurements;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Measurements;

/// <summary>
/// Non-destructive tool that measures distance, delta and angle between two points.
/// </summary>
public sealed class MeasureDistanceTool : TwoPointToolBase, ICommandDrivenTool, IToolPreviewEntityProvider
{
    public override string Name => "Measure Distance";

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State == TwoPointToolState.WaitingForFirstPoint
            ? new CommandPromptState(
                "MEASURE DISTANCE",
                "Specify first point",
                CommandInputKind.Point,
                placeholder: "100,50   |   @50,0")
            : new CommandPromptState(
                "MEASURE DISTANCE",
                "Specify second point",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   distance");
    }


    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? "MEASURE DISTANCE expects a point input.");
        }

        return SubmitResolvedPoint(context, input.Point.Value);
    }

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

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LineEntity? preview = GetPreviewEntity();

        return preview is null
            ? Array.Empty<CadEntity>()
            : new[] { preview };
    }

    protected override ToolResult OnFirstPointSelected(
        ToolContext context,
        Point2D firstPoint)
    {
        return ToolResult.Started("Measure distance: specify second point.");
    }

    protected override ToolResult OnPreviewUpdated(
        ToolContext context,
        Point2D firstPoint,
        Point2D currentPoint)
    {
        DistanceMeasurement measurement = MeasurementService.MeasureDistance(
            firstPoint,
            currentPoint);

        return ToolResult.Updated(MeasurementFormatter.FormatDistance(measurement));
    }

    protected override ToolResult OnSecondPointSelected(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint)
    {
        DistanceMeasurement measurement = MeasurementService.MeasureDistance(
            firstPoint,
            secondPoint);

        return ToolResult.Completed(MeasurementFormatter.FormatDistance(measurement));
    }
}
