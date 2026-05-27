using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Architectural;

/// <summary>
/// Inserts a simple north arrow made of ordinary CAD geometry.
/// </summary>
public sealed class NorthSymbolTool : ICadTool
{
    public const double DefaultSize = 50.0;

    public string Name => "North Symbol";

    public Point2D? LastInsertionPoint { get; private set; }

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
            DefaultSize,
            context.Creation.CurrentLayerId,
            context.Creation.CurrentTextFormatId);

        context.Commands.Execute(
            context.Document,
            new CompositeCommand(
                "Insert North Symbol",
                entities.Select(entity => new AddEntityCommand(entity))));

        LastInsertionPoint = insertionPoint;
        context.CurrentBasePoint = insertionPoint;

        return ToolResult.Completed("North symbol inserted.");
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

        return ToolResult.Cancelled("North symbol cancelled.");
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
        double size,
        LayerId layerId,
        TextFormatId textFormatId)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "North symbol size must be positive.");
        }

        double scale = size / DefaultSize;

        Point2D shaftStart = insertionPoint + new Vector2D(0, 11.35533905932737 * scale);
        Point2D shaftEnd = insertionPoint + new Vector2D(0, -16.0 * scale);
        Point2D arrowTip = insertionPoint + new Vector2D(0, -24.0 * scale);
        Point2D arrowRight = insertionPoint + new Vector2D(17.67766952966369 * scale, -6.322330470336311 * scale);
        Point2D arrowLeft = insertionPoint + new Vector2D(-17.677669529663685 * scale, -6.322330470336311 * scale);
        Point2D circleCenter = insertionPoint + new Vector2D(0, -6.322330470336316 * scale);
        Point2D labelPoint = insertionPoint + new Vector2D(-3.554439814609392 * scale, -35.916367382398974 * scale);

        return new CadEntity[]
        {
            new LineEntity(
                shaftStart,
                shaftEnd,
                layerId: layerId),
            new TextEntity(
                labelPoint,
                "N",
                textFormatId: textFormatId,
                layerId: layerId),
            new LineEntity(
                arrowTip,
                arrowRight,
                layerId: layerId),
            new LineEntity(
                arrowTip,
                arrowLeft,
                layerId: layerId),
            new CircleEntity(
                circleCenter,
                17.677669529663685 * scale,
                layerId: layerId)
        };
    }

    private static Point2D ApplySnap(
        ToolContext context,
        Point2D cursorPoint)
    {
        if (context.EnabledSnaps == SnapKind.None ||
            Tolerance.IsZero(context.SnapTolerance))
        {
            return cursorPoint;
        }

        var request = new SnapRequest(
            context.Document,
            cursorPoint,
            context.SnapTolerance,
            context.EnabledSnaps,
            context.CurrentBasePoint,
            context.GridSettings);

        SnapCandidate? candidate = context.SnapService.Snap(request);

        return candidate?.Point ?? cursorPoint;
    }
}
