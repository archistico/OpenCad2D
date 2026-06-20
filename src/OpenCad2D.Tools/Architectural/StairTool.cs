using OpenCad2D.Core.Architecture.Stairs;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Architectural;

/// <summary>
/// Inserts a persistent parametric straight stair entity.
/// </summary>
public sealed class StairTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider
{
    public const double DefaultWidth = 100.0;
    public const int DefaultTreadCount = 18;
    public const double DefaultTreadDepth = 28.0;
    public const double DefaultRiserHeight = 17.0;
    public const bool DefaultShowStructure = false;
    public const double DefaultSlabThickness = 3.0;
    public const StairPlanArrowMode DefaultPlanArrowMode = StairPlanArrowMode.FirstToLast;
    public const bool DefaultShowPlanSectionMarker = false;

    private static readonly CommandOption PlanOption = new(
        "Plan",
        "P",
        "Insert a plan-view stair");

    private static readonly CommandOption SideOption = new(
        "Side",
        "S",
        "Insert a side-elevation stair");

    private static readonly CommandOption FrontOption = new(
        "Front",
        "F",
        "Insert a front-elevation stair");

    private Point2D? _previewInsertionPoint;

    public string Name => "Stair";

    public Point2D? LastInsertionPoint { get; private set; }

    public StairViewKind CurrentViewKind { get; private set; } = StairViewKind.Plan;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "STAIR",
            $"Specify {FormatViewName(CurrentViewKind)} stair insertion point",
            CommandInputKind.PointOrOption,
            new[] { PlanOption, SideOption, FrontOption },
            placeholder: "click insertion point, enter X/Y or choose Plan/Side/Front");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            return HandleViewOption(input.OptionKeyword);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? $"{Name} expects a point input or Plan/Side/Front option.");
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

        StairEntity stair = CreateDefaultStair(
            insertionPoint,
            context.Creation.CurrentLayerId,
            CurrentViewKind);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(stair));

        LastInsertionPoint = insertionPoint;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = insertionPoint;

        return ToolResult.Completed($"{FormatViewName(CurrentViewKind)} stair inserted.");
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        _previewInsertionPoint = ApplySnap(
            context,
            pointer.ModelPoint);

        return ToolResult.None();
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_previewInsertionPoint is not { } insertionPoint)
        {
            return Array.Empty<CadEntity>();
        }

        return new CadEntity[]
        {
            CreateDefaultStair(
                insertionPoint,
                context.Creation.CurrentLayerId,
                CurrentViewKind)
        };
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastInsertionPoint = null;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.Cancelled("Stair cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastInsertionPoint = null;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.None();
    }

    public static StairEntity CreateDefaultStair(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId)
    {
        return CreateDefaultStair(
            insertionPoint,
            layerId,
            StairViewKind.Plan);
    }

    public static StairEntity CreateDefaultStair(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId,
        StairViewKind viewKind)
    {
        return new StairEntity(
            insertionPoint,
            viewKind,
            DefaultWidth,
            DefaultTreadCount,
            DefaultTreadDepth,
            DefaultRiserHeight,
            DefaultShowStructure,
            DefaultSlabThickness,
            layerId: layerId,
            planArrowMode: DefaultPlanArrowMode,
            showPlanSectionMarker: DefaultShowPlanSectionMarker);
    }

    private ToolResult HandleViewOption(string? optionKeyword)
    {
        if (PlanOption.Matches(optionKeyword ?? string.Empty))
        {
            CurrentViewKind = StairViewKind.Plan;
        }
        else if (SideOption.Matches(optionKeyword ?? string.Empty))
        {
            CurrentViewKind = StairViewKind.SideElevation;
        }
        else if (FrontOption.Matches(optionKeyword ?? string.Empty))
        {
            CurrentViewKind = StairViewKind.FrontElevation;
        }
        else
        {
            return ToolResult.None("Unknown stair view option. Use Plan, Side or Front.");
        }

        return ToolResult.Updated($"Stair view set to {FormatViewName(CurrentViewKind)}.");
    }

    private static string FormatViewName(StairViewKind viewKind)
    {
        return viewKind switch
        {
            StairViewKind.Plan => "Plan",
            StairViewKind.SideElevation => "Side elevation",
            StairViewKind.FrontElevation => "Front elevation",
            _ => viewKind.ToString()
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
