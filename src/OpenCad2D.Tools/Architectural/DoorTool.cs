using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Architecture.Doors;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Architectural;

/// <summary>
/// Inserts a persistent parametric architectural door entity.
/// </summary>
public sealed class DoorTool : ICadTool, ICommandDrivenTool, IToolPreviewEntityProvider
{
    public const double DefaultWidth = 90.0;
    public const double DefaultWallThickness = 20.0;
    public const double DefaultOpeningAngleDegrees = 90.0;
    public const DoorSwingDirection DefaultSwingDirection = DoorSwingDirection.Left;
    public const AnchorPoint DefaultAnchor = AnchorPoint.MiddleLeft;
    public const bool DefaultMaskWallOpening = true;

    private static readonly CommandOption LeftOption = new(
        "Left",
        "L",
        "Draw the door swing on the left side of the closed leaf");

    private static readonly CommandOption RightOption = new(
        "Right",
        "R",
        "Draw the door swing on the right side of the closed leaf");

    private static readonly CommandOption AnchorOption = new(
        "Anchor",
        "A",
        "Keep the current HUD anchor for the next door insertion");

    private static readonly CommandOption MaskOption = new(
        "Mask",
        "M",
        "Toggle the non-destructive wall-opening mask for the next door insertion");

    private Point2D? _previewInsertionPoint;

    public string Name => "Door";

    public Point2D? LastInsertionPoint { get; private set; }

    public DoorSwingDirection CurrentSwingDirection { get; private set; } = DefaultSwingDirection;

    public AnchorPoint CurrentAnchor { get; private set; } = DefaultAnchor;

    public bool CurrentMaskWallOpening { get; private set; } = DefaultMaskWallOpening;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "DOOR",
            "Specify door insertion point",
            CommandInputKind.PointOrOption,
            new[] { LeftOption, RightOption, AnchorOption, MaskOption },
            placeholder: "click insertion point, enter X/Y or choose Left/Right/Anchor/Mask");
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
                input.ErrorMessage ?? "Door expects a point input or Left/Right/Anchor/Mask option.");
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

        DoorEntity door = CreateDefaultDoor(
            insertionPoint,
            context.Creation.CurrentLayerId,
            CurrentSwingDirection,
            CurrentAnchor,
            CurrentMaskWallOpening);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(door));

        LastInsertionPoint = insertionPoint;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = insertionPoint;

        return ToolResult.Completed("Door inserted.");
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
            CreateDefaultDoor(
                insertionPoint,
                context.Creation.CurrentLayerId,
                CurrentSwingDirection,
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

        return ToolResult.Cancelled("Door cancelled.");
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

        return ToolResult.Updated($"Door anchor set to {descriptor.DisplayName}.");
    }

    public static DoorEntity CreateDefaultDoor(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId)
    {
        return CreateDefaultDoor(
            insertionPoint,
            layerId,
            DefaultSwingDirection,
            DefaultAnchor,
            DefaultMaskWallOpening);
    }

    public static DoorEntity CreateDefaultDoor(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId,
        DoorSwingDirection swingDirection,
        AnchorPoint anchor,
        bool maskWallOpening)
    {
        return new DoorEntity(
            insertionPoint,
            DefaultWidth,
            DefaultWallThickness,
            DefaultOpeningAngleDegrees,
            swingDirection,
            anchor,
            maskWallOpening,
            layerId: layerId);
    }

    private ToolResult HandleOption(string? optionKeyword)
    {
        if (LeftOption.Matches(optionKeyword ?? string.Empty))
        {
            CurrentSwingDirection = DoorSwingDirection.Left;
            return ToolResult.Updated("Door swing set to Left.");
        }

        if (RightOption.Matches(optionKeyword ?? string.Empty))
        {
            CurrentSwingDirection = DoorSwingDirection.Right;
            return ToolResult.Updated("Door swing set to Right.");
        }

        if (AnchorOption.Matches(optionKeyword ?? string.Empty))
        {
            return ToolResult.Updated("Use the HUD 3x3 anchor selector or numeric shortcuts 1-9 to choose the door anchor.");
        }

        if (MaskOption.Matches(optionKeyword ?? string.Empty))
        {
            CurrentMaskWallOpening = !CurrentMaskWallOpening;
            return ToolResult.Updated(CurrentMaskWallOpening
                ? "Door wall mask enabled."
                : "Door wall mask disabled.");
        }

        return ToolResult.None("Unknown door option. Use Left, Right, Anchor or Mask.");
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
