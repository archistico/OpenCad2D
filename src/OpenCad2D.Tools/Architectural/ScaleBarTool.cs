using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Architectural;

/// <summary>
/// Inserts a metric graphic scale bar made of ordinary CAD geometry.
/// </summary>
public sealed class ScaleBarTool : ICadTool, ICommandDrivenTool
{
    public const double DefaultScaleLength = 1000.0;

    public string Name => "Metric Scale Bar";

    public Point2D? LastInsertionPoint { get; private set; }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "METRIC SCALE BAR",
            "Specify insertion point",
            CommandInputKind.Point,
            placeholder: "100,50   |   @50,0");
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
                input.ErrorMessage ?? $"{Name} expects a point input.");
        }

        return OnPointerPressed(
            context,
            new PointerInfo(input.Point.Value));
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D insertionPoint = ApplySnap(
            context,
            pointer.ModelPoint);

        IReadOnlyList<CadEntity> entities = CreateEntities(
            insertionPoint,
            context.Creation.CurrentLayerId,
            context.Creation.CurrentTextFormatId);

        context.Commands.Execute(
            context.Document,
            new CompositeCommand(
                "Insert Metric Scale Bar",
                entities.Select(entity => new AddEntityCommand(entity))));

        LastInsertionPoint = insertionPoint;
        context.CurrentBasePoint = insertionPoint;

        return ToolResult.Completed("Metric scale bar inserted.");
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return ToolResult.None();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.Cancelled("Metric scale bar cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.None();
    }

    public static IReadOnlyList<CadEntity> CreateEntities(
        Point2D insertionPoint,
        LayerId layerId,
        TextFormatId textFormatId)
    {
        var entities = new List<CadEntity>
        {
            CreateFilledRectangle(insertionPoint, layerId, 0, 20, 100, 0, isFilled: true),
            CreateFilledRectangle(insertionPoint, layerId, 100, 0, 200, -20, isFilled: false),
            CreateFilledRectangle(insertionPoint, layerId, 200, 20, 300, 0, isFilled: true),
            CreateFilledRectangle(insertionPoint, layerId, 300, 0, 400, -20, isFilled: false),
            CreateFilledRectangle(insertionPoint, layerId, 400, 20, 500, 0, isFilled: true),
            CreateFilledRectangle(insertionPoint, layerId, 500, 0, 1000, -20, isFilled: false),

            CreateVerticalTick(insertionPoint, layerId, 0),
            CreateVerticalTick(insertionPoint, layerId, 100),
            CreateVerticalTick(insertionPoint, layerId, 200),
            CreateVerticalTick(insertionPoint, layerId, 300),
            CreateVerticalTick(insertionPoint, layerId, 400),
            CreateVerticalTick(insertionPoint, layerId, 500),
            CreateVerticalTick(insertionPoint, layerId, 1000),

            CreateLabel(insertionPoint, layerId, textFormatId, "0", 0),
            CreateLabel(insertionPoint, layerId, textFormatId, "100", 100),
            CreateLabel(insertionPoint, layerId, textFormatId, "200", 200),
            CreateLabel(insertionPoint, layerId, textFormatId, "300", 300),
            CreateLabel(insertionPoint, layerId, textFormatId, "400", 400),
            CreateLabel(insertionPoint, layerId, textFormatId, "500", 500),
            CreateLabel(insertionPoint, layerId, textFormatId, "1000", 1000)
        };

        return entities;
    }

    private static PolylineEntity CreateFilledRectangle(
        Point2D insertionPoint,
        LayerId layerId,
        double x1,
        double y1,
        double x2,
        double y2,
        bool isFilled)
    {
        return new PolylineEntity(
            new[]
            {
                Translate(insertionPoint, x1, y1),
                Translate(insertionPoint, x2, y1),
                Translate(insertionPoint, x2, y2),
                Translate(insertionPoint, x1, y2)
            },
            isClosed: true,
            layerId: layerId,
            isFilled: isFilled);
    }

    private static LineEntity CreateVerticalTick(
        Point2D insertionPoint,
        LayerId layerId,
        double x)
    {
        return new LineEntity(
            Translate(insertionPoint, x, 30),
            Translate(insertionPoint, x, -30),
            layerId: layerId);
    }

    private static TextEntity CreateLabel(
        Point2D insertionPoint,
        LayerId layerId,
        TextFormatId textFormatId,
        string text,
        double x)
    {
        return new TextEntity(
            Translate(insertionPoint, x, -50),
            text,
            textFormatId: textFormatId,
            layerId: layerId);
    }

    private static Point2D Translate(
        Point2D insertionPoint,
        double x,
        double y)
    {
        return insertionPoint + new Vector2D(x, y);
    }

    private static Point2D ApplySnap(
        ToolContext context,
        Point2D cursorPoint)
    {
        SnapKind enabledSnaps = context.EnabledSnaps & ~SnapKind.Entity;

        if (enabledSnaps == SnapKind.None ||
            Tolerance.IsZero(context.SnapTolerance))
        {
            return cursorPoint;
        }

        var request = new SnapRequest(
            context.Document,
            cursorPoint,
            context.SnapTolerance,
            enabledSnaps,
            context.CurrentBasePoint,
            context.GridSettings);

        SnapCandidate? candidate = context.SnapService.Snap(request);

        return candidate?.Point ?? cursorPoint;
    }
}
