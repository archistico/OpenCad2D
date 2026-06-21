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

    private static readonly CommandOption WidthOption = new(
        "Width",
        "W",
        "Set the default width for the next door insertion");

    private static readonly CommandOption ThicknessOption = new(
        "Thickness",
        "T",
        "Set the default wall thickness for the next door insertion");

    private static readonly CommandOption OpeningOption = new(
        "Opening",
        "O",
        "Set the default opening angle for the next door insertion");

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

    public DoorToolState State { get; private set; } = DoorToolState.WaitingForInsertionPoint;

    public Point2D? LastInsertionPoint { get; private set; }

    public double CurrentWidth { get; private set; } = DefaultWidth;

    public double CurrentWallThickness { get; private set; } = DefaultWallThickness;

    public double CurrentOpeningAngleDegrees { get; private set; } = DefaultOpeningAngleDegrees;

    public DoorSwingDirection CurrentSwingDirection { get; private set; } = DefaultSwingDirection;

    public AnchorPoint CurrentAnchor { get; private set; } = DefaultAnchor;

    public bool CurrentMaskWallOpening { get; private set; } = DefaultMaskWallOpening;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            DoorToolState.WaitingForWidth => new CommandPromptState(
                "DOOR",
                $"Specify door width <{CurrentWidth:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Width, for example 90"),

            DoorToolState.WaitingForWallThickness => new CommandPromptState(
                "DOOR",
                $"Specify door wall thickness <{CurrentWallThickness:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Wall thickness, for example 20"),

            DoorToolState.WaitingForOpeningAngle => new CommandPromptState(
                "DOOR",
                $"Specify door opening angle <{CurrentOpeningAngleDegrees:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Opening angle, for example 90"),

            _ => new CommandPromptState(
                "DOOR",
                $"Specify door insertion point [Width/Thickness/Opening/Left/Right/Anchor/Mask] <W={CurrentWidth:0.###}, T={CurrentWallThickness:0.###}, O={CurrentOpeningAngleDegrees:0.###}, S={CurrentSwingDirection}, A={FormatAnchor(CurrentAnchor)}, M={FormatMaskState(CurrentMaskWallOpening)}>",
                CommandInputKind.PointOrOption,
                new[] { WidthOption, ThicknessOption, OpeningOption, LeftOption, RightOption, AnchorOption, MaskOption },
                placeholder: "click insertion point, enter X/Y or choose W/T/O/L/R/A/M")
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == DoorToolState.WaitingForWidth)
        {
            return HandleWidthInput(input, context);
        }

        if (State == DoorToolState.WaitingForWallThickness)
        {
            return HandleWallThicknessInput(input, context);
        }

        if (State == DoorToolState.WaitingForOpeningAngle)
        {
            return HandleOpeningAngleInput(input, context);
        }

        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            return HandleOption(input.OptionKeyword, context);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? "Door expects a point input or Width/Thickness/Opening/Left/Right/Anchor/Mask option.");
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

        if (State != DoorToolState.WaitingForInsertionPoint)
        {
            return ToolResult.None("Finish the current door option before inserting the door.");
        }

        Point2D insertionPoint = ApplySnap(
            context,
            pointer.ModelPoint);

        DoorEntity door = CreateDoor(
            insertionPoint,
            context.Creation.CurrentLayerId,
            CurrentWidth,
            CurrentWallThickness,
            CurrentOpeningAngleDegrees,
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

        if (State != DoorToolState.WaitingForInsertionPoint)
        {
            _previewInsertionPoint = null;
            return ToolResult.None();
        }

        _previewInsertionPoint = ApplySnap(
            context,
            pointer.ModelPoint);

        return ToolResult.None();
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_previewInsertionPoint is not { } insertionPoint ||
            State != DoorToolState.WaitingForInsertionPoint)
        {
            return Array.Empty<CadEntity>();
        }

        return new CadEntity[]
        {
            CreateDoor(
                insertionPoint,
                context.Creation.CurrentLayerId,
                CurrentWidth,
                CurrentWallThickness,
                CurrentOpeningAngleDegrees,
                CurrentSwingDirection,
                CurrentAnchor,
                CurrentMaskWallOpening)
        };
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        State = DoorToolState.WaitingForInsertionPoint;
        LastInsertionPoint = null;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.Cancelled("Door cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        State = DoorToolState.WaitingForInsertionPoint;
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
        return CreateDoor(
            insertionPoint,
            layerId,
            DefaultWidth,
            DefaultWallThickness,
            DefaultOpeningAngleDegrees,
            swingDirection,
            anchor,
            maskWallOpening);
    }

    public static DoorEntity CreateDoor(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId,
        double width,
        double wallThickness,
        double openingAngleDegrees,
        DoorSwingDirection swingDirection,
        AnchorPoint anchor,
        bool maskWallOpening)
    {
        return new DoorEntity(
            insertionPoint,
            width,
            wallThickness,
            openingAngleDegrees,
            swingDirection,
            anchor,
            maskWallOpening,
            layerId: layerId);
    }


    private static string FormatAnchor(AnchorPoint anchor)
    {
        return AnchorPointService.GetDescriptor(anchor).DisplayName;
    }

    private static string FormatMaskState(bool maskWallOpening)
    {
        return maskWallOpening ? "On" : "Off";
    }

    private ToolResult HandleOption(string? optionKeyword, ToolContext context)
    {
        string option = optionKeyword ?? string.Empty;

        if (WidthOption.Matches(option))
        {
            State = DoorToolState.WaitingForWidth;
            context.CurrentBasePoint = null;
            _previewInsertionPoint = null;
            return ToolResult.Started($"Specify door width <{CurrentWidth:0.###}>.");
        }

        if (ThicknessOption.Matches(option))
        {
            State = DoorToolState.WaitingForWallThickness;
            context.CurrentBasePoint = null;
            _previewInsertionPoint = null;
            return ToolResult.Started($"Specify door wall thickness <{CurrentWallThickness:0.###}>.");
        }

        if (OpeningOption.Matches(option))
        {
            State = DoorToolState.WaitingForOpeningAngle;
            context.CurrentBasePoint = null;
            _previewInsertionPoint = null;
            return ToolResult.Started($"Specify door opening angle <{CurrentOpeningAngleDegrees:0.###}>.");
        }

        if (LeftOption.Matches(option))
        {
            CurrentSwingDirection = DoorSwingDirection.Left;
            return ToolResult.Updated("Door swing set to Left.");
        }

        if (RightOption.Matches(option))
        {
            CurrentSwingDirection = DoorSwingDirection.Right;
            return ToolResult.Updated("Door swing set to Right.");
        }

        if (AnchorOption.Matches(option))
        {
            return ToolResult.Updated("Use the HUD 3x3 anchor selector or numeric shortcuts 1-9 to choose the door anchor.");
        }

        if (MaskOption.Matches(option))
        {
            CurrentMaskWallOpening = !CurrentMaskWallOpening;
            return ToolResult.Updated(CurrentMaskWallOpening
                ? "Door wall mask enabled."
                : "Door wall mask disabled.");
        }

        return ToolResult.None("Unknown door option. Use Width, Thickness, Opening, Left, Right, Anchor or Mask.");
    }

    private ToolResult HandleWidthInput(CommandInputSubmission input, ToolContext context)
    {
        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            State = DoorToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Door width remains {CurrentWidth:0.###}. Specify insertion point.");
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            if (input.Number.Value <= 0.0)
            {
                return ToolResult.None("Door width must be greater than zero.");
            }

            CurrentWidth = input.Number.Value;
            State = DoorToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Door width set to {CurrentWidth:0.###}. Specify insertion point.");
        }

        return ToolResult.None("Specify a positive door width.");
    }

    private ToolResult HandleWallThicknessInput(CommandInputSubmission input, ToolContext context)
    {
        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            State = DoorToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Door wall thickness remains {CurrentWallThickness:0.###}. Specify insertion point.");
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            if (input.Number.Value <= 0.0)
            {
                return ToolResult.None("Door wall thickness must be greater than zero.");
            }

            CurrentWallThickness = input.Number.Value;
            State = DoorToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Door wall thickness set to {CurrentWallThickness:0.###}. Specify insertion point.");
        }

        return ToolResult.None("Specify a positive door wall thickness.");
    }

    private ToolResult HandleOpeningAngleInput(CommandInputSubmission input, ToolContext context)
    {
        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            State = DoorToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Door opening angle remains {CurrentOpeningAngleDegrees:0.###}. Specify insertion point.");
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            double angle = input.Number.Value;
            if (angle <= 0.0 || angle > 180.0)
            {
                return ToolResult.None("Door opening angle must be greater than zero and no more than 180 degrees.");
            }

            CurrentOpeningAngleDegrees = angle;
            State = DoorToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Door opening angle set to {CurrentOpeningAngleDegrees:0.###}. Specify insertion point.");
        }

        return ToolResult.None("Specify a door opening angle greater than zero and no more than 180 degrees.");
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

public enum DoorToolState
{
    WaitingForInsertionPoint,
    WaitingForWidth,
    WaitingForWallThickness,
    WaitingForOpeningAngle
}
