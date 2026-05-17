using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Measurements;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Measurements;

/// <summary>
/// Non-destructive tool that measures distance, delta and angle between two points.
/// </summary>
public sealed class MeasureDistanceTool : TwoPointToolBase, IToolPreviewEntityProvider
{
    public override string Name => "Measure Distance";

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
