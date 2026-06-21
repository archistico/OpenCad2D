using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Architectural;

/// <summary>
/// Inserts a persistent parametric architectural window entity.
/// </summary>
public sealed class WindowTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider
{
    public const double DefaultWidth = 120.0;
    public const double DefaultWallThickness = 20.0;
    public const double DefaultFrameOffset = 4.0;
    public const AnchorPoint DefaultAnchor = AnchorPoint.MiddleLeft;
    public const bool DefaultMaskWallOpening = true;

    private static readonly CommandOption AnchorOption = new(
        "Anchor",
        "A",
        "Keep the current HUD anchor for the next window insertion");

    private static readonly CommandOption MaskOption = new(
        "Mask",
        "M",
        "Toggle the non-destructive wall-opening mask for the next window insertion");

    private Point2D? _previewInsertionPoint;

    public string Name => "Window";

    public Point2D? LastInsertionPoint { get; private set; }

    public AnchorPoint CurrentAnchor { get; private set; } = DefaultAnchor;

    public bool CurrentMaskWallOpening { get; private set; } = DefaultMaskWallOpening;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "WINDOW",
            "Specify window insertion point",
            CommandInputKind.PointOrOption,
            new[] { AnchorOption, MaskOption },
            placeholder: "click insertion point, enter X/Y or choose Anchor/Mask");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            return HandleOption(input.OptionKeyword);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? "Window expects a point input or Anchor/Mask option.");
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

        WindowEntity window = CreateDefaultWindow(
            insertionPoint,
            context.Creation.CurrentLayerId,
            CurrentAnchor,
            CurrentMaskWallOpening);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(window));

        LastInsertionPoint = insertionPoint;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = insertionPoint;

        return ToolResult.Completed("Window inserted.");
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
            CreateDefaultWindow(
                insertionPoint,
                context.Creation.CurrentLayerId,
                CurrentAnchor,
                CurrentMaskWallOpening)
        };
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastInsertionPoint = null;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.Cancelled("Window cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastInsertionPoint = null;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.None();
    }

    public ToolResult SetAnchor(AnchorPoint anchor)
    {
        CurrentAnchor = anchor;
        AnchorPointDescriptor descriptor = AnchorPointService.GetDescriptor(anchor);

        return ToolResult.Updated($"Window anchor set to {descriptor.DisplayName}.");
    }

    public static WindowEntity CreateDefaultWindow(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId)
    {
        return CreateDefaultWindow(
            insertionPoint,
            layerId,
            DefaultAnchor,
            DefaultMaskWallOpening);
    }

    public static WindowEntity CreateDefaultWindow(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId,
        AnchorPoint anchor,
        bool maskWallOpening)
    {
        return new WindowEntity(
            insertionPoint,
            DefaultWidth,
            DefaultWallThickness,
            DefaultFrameOffset,
            anchor,
            maskWallOpening,
            layerId: layerId);
    }

    private ToolResult HandleOption(string? optionKeyword)
    {
        if (AnchorOption.Matches(optionKeyword ?? string.Empty))
        {
            return ToolResult.Updated("Use the HUD 3x3 anchor selector or numeric shortcuts 1-9 to choose the window anchor.");
        }

        if (MaskOption.Matches(optionKeyword ?? string.Empty))
        {
            CurrentMaskWallOpening = !CurrentMaskWallOpening;
            return ToolResult.Updated(CurrentMaskWallOpening
                ? "Window wall mask enabled."
                : "Window wall mask disabled.");
        }

        return ToolResult.None("Unknown window option. Use Anchor or Mask.");
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
