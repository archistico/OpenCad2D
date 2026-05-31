using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// AutoCAD-style DIVIDE command: places persistent point entities at equal divisions
/// without modifying the source curve.
/// </summary>
public sealed class DivideTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    public const int DefaultSegmentCount = 2;
    public const int MinimumSegmentCount = DivideEntityService.MinimumSegmentCount;
    public const int MaximumSegmentCount = DivideEntityService.MaximumSegmentCount;

    private readonly DivideEntityService _divideService = new();
    private EntityId? _targetEntityId;

    public string Name => "Divide";

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _targetEntityId is null && TryGetSingleSelectedDividableEntity(context) is null
            ? SnapKind.EntityOnly
            : SnapKind.None;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        EntityId? targetId = GetTargetEntityId(context);

        if (targetId is null)
        {
            return new CommandPromptState(
                "DIVIDE",
                "Select entity to divide",
                CommandInputKind.Selection,
                placeholder: "select one line, arc, circle or polyline");
        }

        return new CommandPromptState(
            "DIVIDE",
            $"Enter number of segments <{DefaultSegmentCount}>",
            CommandInputKind.Number,
            acceptsEmptyEnter: true,
            placeholder: $"{MinimumSegmentCount}-{MaximumSegmentCount}");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        EntityId? targetId = GetTargetEntityId(context);

        if (targetId is null)
        {
            return ToolResult.None("Select one entity to divide.");
        }

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return ExecuteDivide(
                context,
                targetId.Value,
                DefaultSegmentCount);
        }

        if (input.Kind != CommandInputSubmissionKind.Number || input.Number is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? "DIVIDE expects an integer segment count.");
        }

        double rawNumber = input.Number.Value;
        if (!double.IsFinite(rawNumber))
        {
            return ToolResult.None("Segment count must be an integer between 2 and 1000.");
        }

        double roundedNumber = Math.Round(rawNumber);
        if (Math.Abs(rawNumber - roundedNumber) > 1e-9 ||
            roundedNumber < int.MinValue ||
            roundedNumber > int.MaxValue)
        {
            return ToolResult.None("Segment count must be an integer between 2 and 1000.");
        }

        return ExecuteDivide(
            context,
            targetId.Value,
            (int)roundedNumber);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (GetTargetEntityId(context) is not null)
        {
            return ToolResult.None("Enter the number of segments for DIVIDE.");
        }

        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return ToolResult.None("Select one entity to divide.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);
        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        if (!_divideService.CanDivide(entity))
        {
            return ToolResult.None("Selected entity cannot be divided.");
        }

        _targetEntityId = selectedId.Value;
        return ToolResult.Updated("Entity selected. Enter the number of segments.");
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

        _targetEntityId = null;
        return ToolResult.Cancelled("DIVIDE cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _targetEntityId = null;
        return ToolResult.None();
    }

    private ToolResult ExecuteDivide(
        ToolContext context,
        EntityId targetId,
        int segmentCount)
    {
        CadEntity source = context.Document.Entities.GetRequired(targetId);
        if (!context.Document.IsEntitySelectable(source))
        {
            _targetEntityId = null;
            return ToolResult.None("Target entity is not editable.");
        }

        DivideEntityResult result = _divideService.Divide(
            source,
            segmentCount);

        if (!result.Succeeded)
        {
            return ToolResult.None(result.Message);
        }

        IReadOnlyList<PointEntity> pointEntities = result.Points
            .Select(point => new PointEntity(
                point,
                layerId: context.Creation.CurrentLayerId))
            .ToList();

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(pointEntities));

        _targetEntityId = null;

        string sourceName = source.Kind.ToString();
        string pointText = pointEntities.Count == 1
            ? "1 point"
            : $"{pointEntities.Count} points";

        return ToolResult.Completed(
            $"DIVIDE created {pointText} on {sourceName}.");
    }

    public bool IsWaitingForSegmentCount(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetTargetEntityId(context) is not null;
    }

    private EntityId? GetTargetEntityId(ToolContext context)
    {
        if (_targetEntityId is not null &&
            context.Document.Entities.TryGet(_targetEntityId.Value, out CadEntity? explicitTarget) &&
            explicitTarget is not null &&
            context.Document.IsEntitySelectable(explicitTarget) &&
            _divideService.CanDivide(explicitTarget))
        {
            return _targetEntityId.Value;
        }

        _targetEntityId = null;
        return TryGetSingleSelectedDividableEntity(context);
    }

    private EntityId? TryGetSingleSelectedDividableEntity(ToolContext context)
    {
        if (context.Selection.SelectedIds.Count != 1)
        {
            return null;
        }

        EntityId selectedId = context.Selection.SelectedIds.First();
        if (!context.Document.Entities.TryGet(selectedId, out CadEntity? entity) ||
            entity is null ||
            !context.Document.IsEntitySelectable(entity) ||
            !_divideService.CanDivide(entity))
        {
            return null;
        }

        return selectedId;
    }
}
