using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw rectangular closed polylines.
/// </summary>
public sealed class RectangleTool : TwoPointToolBase
{
    public override string Name => "Rectangle";

    public PolylineEntity? GetPreviewEntity()
    {
        if (!HasPreview || FirstPoint is null || CurrentPoint is null)
        {
            return null;
        }

        if (!CanCreateRectangle(
            FirstPoint.Value,
            CurrentPoint.Value,
            GeometryTolerance.Default))
        {
            return null;
        }

        return CreatePreviewRectangleEntity(
            FirstPoint.Value,
            CurrentPoint.Value);
    }

    private static PolylineEntity CreatePreviewRectangleEntity(
        Point2D firstCorner,
        Point2D oppositeCorner)
    {
        return CreateRectangleEntity(
            firstCorner,
            oppositeCorner,
            LayerId.Default);
    }

    protected override ToolResult OnFirstPointSelected(
        ToolContext context,
        Point2D firstPoint)
    {
        return ToolResult.Started("Specify opposite corner.");
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
        if (!CanCreateRectangle(firstPoint, secondPoint, context))
        {
            return ToolResult.None("Rectangle width and height must be greater than zero.");
        }

        PolylineEntity rectangle = CreateRectangleEntity(
            firstPoint,
            secondPoint,
            context.CurrentLayerId);

        context.CommandHistory.Execute(
            context.Document,
            new AddEntityCommand(rectangle));

        return ToolResult.Completed("Rectangle created.");
    }

    protected override bool ShouldResetAfterSecondPoint(ToolResult result)
    {
        return result.Kind == ToolResultKind.Completed;
    }

    private static bool CanCreateRectangle(
    Point2D firstCorner,
    Point2D oppositeCorner,
    ToolContext context)
    {
        return CanCreateRectangle(
            firstCorner,
            oppositeCorner,
            context.GeometryTolerance);
    }

    private static bool CanCreateRectangle(
        Point2D firstCorner,
        Point2D oppositeCorner,
        GeometryTolerance tolerance)
    {
        return !tolerance.AreDistancesEqual(firstCorner.X, oppositeCorner.X)
            && !tolerance.AreDistancesEqual(firstCorner.Y, oppositeCorner.Y);
    }

    private static bool IsValidRectangle(
        Point2D firstCorner,
        Point2D oppositeCorner,
        ToolContext context)
    {
        return !context.GeometryTolerance.AreDistancesEqual(firstCorner.X, oppositeCorner.X)
            && !context.GeometryTolerance.AreDistancesEqual(firstCorner.Y, oppositeCorner.Y);
    }

    private static PolylineEntity CreateRectangleEntity(
        Point2D firstCorner,
        Point2D oppositeCorner,
        LayerId layerId)
    {
        var vertices = new[]
        {
        firstCorner,
        new Point2D(oppositeCorner.X, firstCorner.Y),
        oppositeCorner,
        new Point2D(firstCorner.X, oppositeCorner.Y)
    };

        return new PolylineEntity(
            vertices,
            isClosed: true,
            layerId: layerId);
    }
}