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

    private static readonly CommandOption WidthOption = new(
        "Width",
        "W",
        "Set the default width for the next window insertion");

    private static readonly CommandOption ThicknessOption = new(
        "Thickness",
        "T",
        "Set the default wall thickness for the next window insertion");

    private static readonly CommandOption OffsetOption = new(
        "Offset",
        "O",
        "Set the default frame offset for the next window insertion");

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

    public WindowToolState State { get; private set; } = WindowToolState.WaitingForInsertionPoint;

    public Point2D? LastInsertionPoint { get; private set; }

    public double CurrentWidth { get; private set; } = DefaultWidth;

    public double CurrentWallThickness { get; private set; } = DefaultWallThickness;

    public double CurrentFrameOffset { get; private set; } = DefaultFrameOffset;

    public AnchorPoint CurrentAnchor { get; private set; } = DefaultAnchor;

    public bool CurrentMaskWallOpening { get; private set; } = DefaultMaskWallOpening;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State switch
        {
            WindowToolState.WaitingForWidth => new CommandPromptState(
                "WINDOW",
                $"Specify window width <{CurrentWidth:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Width, for example 120"),

            WindowToolState.WaitingForWallThickness => new CommandPromptState(
                "WINDOW",
                $"Specify window wall thickness <{CurrentWallThickness:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Wall thickness, for example 20"),

            WindowToolState.WaitingForFrameOffset => new CommandPromptState(
                "WINDOW",
                $"Specify window frame offset <{CurrentFrameOffset:0.###}>",
                CommandInputKind.Number,
                acceptsEmptyEnter: true,
                placeholder: "Frame offset, for example 4"),

            _ => new CommandPromptState(
                "WINDOW",
                $"Specify window insertion point [Width/Thickness/Offset/Anchor/Mask] <W={CurrentWidth:0.###}, T={CurrentWallThickness:0.###}, O={CurrentFrameOffset:0.###}, A={FormatAnchor(CurrentAnchor)}, M={FormatMaskState(CurrentMaskWallOpening)}>",
                CommandInputKind.PointOrOption,
                new[] { WidthOption, ThicknessOption, OffsetOption, AnchorOption, MaskOption },
                placeholder: "click insertion point, enter X/Y or choose W/T/O/A/M")
        };
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (State == WindowToolState.WaitingForWidth)
        {
            return HandleWidthInput(input, context);
        }

        if (State == WindowToolState.WaitingForWallThickness)
        {
            return HandleWallThicknessInput(input, context);
        }

        if (State == WindowToolState.WaitingForFrameOffset)
        {
            return HandleFrameOffsetInput(input, context);
        }

        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            return HandleOption(input.OptionKeyword, context);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? "Window expects a point input or Width/Thickness/Offset/Anchor/Mask option.");
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

        if (State != WindowToolState.WaitingForInsertionPoint)
        {
            return ToolResult.None("Finish the current window option before inserting the window.");
        }

        Point2D insertionPoint = ApplySnap(
            context,
            pointer.ModelPoint);

        WindowEntity window = CreateWindow(
            insertionPoint,
            context.Creation.CurrentLayerId,
            CurrentWidth,
            CurrentWallThickness,
            CurrentFrameOffset,
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

        if (State != WindowToolState.WaitingForInsertionPoint)
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
            State != WindowToolState.WaitingForInsertionPoint)
        {
            return Array.Empty<CadEntity>();
        }

        return new CadEntity[]
        {
            CreateWindow(
                insertionPoint,
                context.Creation.CurrentLayerId,
                CurrentWidth,
                CurrentWallThickness,
                CurrentFrameOffset,
                CurrentAnchor,
                CurrentMaskWallOpening)
        };
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        State = WindowToolState.WaitingForInsertionPoint;
        LastInsertionPoint = null;
        _previewInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.Cancelled("Window cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        State = WindowToolState.WaitingForInsertionPoint;
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
        return CreateWindow(
            insertionPoint,
            layerId,
            DefaultWidth,
            DefaultWallThickness,
            DefaultFrameOffset,
            anchor,
            maskWallOpening);
    }

    public static WindowEntity CreateWindow(
        Point2D insertionPoint,
        OpenCad2D.Core.Identifiers.LayerId layerId,
        double width,
        double wallThickness,
        double frameOffset,
        AnchorPoint anchor,
        bool maskWallOpening)
    {
        return new WindowEntity(
            insertionPoint,
            width,
            wallThickness,
            frameOffset,
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
            State = WindowToolState.WaitingForWidth;
            context.CurrentBasePoint = null;
            _previewInsertionPoint = null;
            return ToolResult.Started($"Specify window width <{CurrentWidth:0.###}>.");
        }

        if (ThicknessOption.Matches(option))
        {
            State = WindowToolState.WaitingForWallThickness;
            context.CurrentBasePoint = null;
            _previewInsertionPoint = null;
            return ToolResult.Started($"Specify window wall thickness <{CurrentWallThickness:0.###}>.");
        }

        if (OffsetOption.Matches(option))
        {
            State = WindowToolState.WaitingForFrameOffset;
            context.CurrentBasePoint = null;
            _previewInsertionPoint = null;
            return ToolResult.Started($"Specify window frame offset <{CurrentFrameOffset:0.###}>.");
        }

        if (AnchorOption.Matches(option))
        {
            return ToolResult.Updated("Use the HUD 3x3 anchor selector or numeric shortcuts 1-9 to choose the window anchor.");
        }

        if (MaskOption.Matches(option))
        {
            CurrentMaskWallOpening = !CurrentMaskWallOpening;
            return ToolResult.Updated(CurrentMaskWallOpening
                ? "Window wall mask enabled."
                : "Window wall mask disabled.");
        }

        return ToolResult.None("Unknown window option. Use Width, Thickness, Offset, Anchor or Mask.");
    }

    private ToolResult HandleWidthInput(CommandInputSubmission input, ToolContext context)
    {
        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            State = WindowToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Window width remains {CurrentWidth:0.###}. Specify insertion point.");
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            if (input.Number.Value <= 0.0)
            {
                return ToolResult.None("Window width must be greater than zero.");
            }

            CurrentWidth = input.Number.Value;
            State = WindowToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Window width set to {CurrentWidth:0.###}. Specify insertion point.");
        }

        return ToolResult.None("Specify a positive window width.");
    }

    private ToolResult HandleWallThicknessInput(CommandInputSubmission input, ToolContext context)
    {
        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            State = WindowToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Window wall thickness remains {CurrentWallThickness:0.###}. Specify insertion point.");
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            double wallThickness = input.Number.Value;
            if (wallThickness <= 0.0)
            {
                return ToolResult.None("Window wall thickness must be greater than zero.");
            }

            if (CurrentFrameOffset > wallThickness / 2.0)
            {
                return ToolResult.None("Window wall thickness must be at least twice the current frame offset.");
            }

            CurrentWallThickness = wallThickness;
            State = WindowToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Window wall thickness set to {CurrentWallThickness:0.###}. Specify insertion point.");
        }

        return ToolResult.None("Specify a positive window wall thickness.");
    }

    private ToolResult HandleFrameOffsetInput(CommandInputSubmission input, ToolContext context)
    {
        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            State = WindowToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Window frame offset remains {CurrentFrameOffset:0.###}. Specify insertion point.");
        }

        if (input.Kind == CommandInputSubmissionKind.Number && input.Number is not null)
        {
            double frameOffset = input.Number.Value;
            if (frameOffset <= 0.0)
            {
                return ToolResult.None("Window frame offset must be greater than zero.");
            }

            if (frameOffset > CurrentWallThickness / 2.0)
            {
                return ToolResult.None("Window frame offset cannot be greater than half the wall thickness.");
            }

            CurrentFrameOffset = frameOffset;
            State = WindowToolState.WaitingForInsertionPoint;
            return ToolResult.Started($"Window frame offset set to {CurrentFrameOffset:0.###}. Specify insertion point.");
        }

        return ToolResult.None("Specify a positive window frame offset.");
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

public enum WindowToolState
{
    WaitingForInsertionPoint,
    WaitingForWidth,
    WaitingForWallThickness,
    WaitingForFrameOffset
}
