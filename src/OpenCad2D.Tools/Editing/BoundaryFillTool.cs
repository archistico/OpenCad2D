using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates a filled closed polyline from the boundary around a picked point.
/// </summary>
public sealed class BoundaryFillTool :
    ICadTool,
    ICommandDrivenTool,
    IKeyboardAwareTool,
    IToolPreviewEntityProvider,
    IToolPreviewDescriptorProvider,
    ISnapModeProvider
{
    public const double DefaultGapTolerance = 0.5;

    private readonly BoundaryFillService _boundaryFillService = new();
    private BoundaryFillResult? _previewResult;

    public string Name => "Boundary Fill";

    public bool HasPreview =>
        _previewResult is { Succeeded: true, Polyline: not null };

    public BoundaryFillResult? PreviewResult => _previewResult;

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.Grid;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (HasPreview)
        {
            return new CommandPromptState(
                "BFILL",
                BuildPreviewPrompt(_previewResult!),
                CommandInputKind.Point,
                acceptsEmptyEnter: true,
                placeholder: "Enter/right-click to confirm or pick another point");
        }

        return new CommandPromptState(
            "BFILL",
            "Pick inside a closed boundary",
            CommandInputKind.Point,
            placeholder: "click inside boundary or 100,50");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return ConfirmPreview(context);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "BFILL expects a point inside a closed boundary.");
        }

        return UpdatePreview(
            context,
            input.Point.Value);
    }

    public bool TryHandleKey(
        ToolContext context,
        CadToolKey key,
        out ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (key == CadToolKey.Enter)
        {
            result = ConfirmPreview(context);
            return result.Changed;
        }

        result = ToolResult.None();
        return false;
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return UpdatePreview(
            context,
            pointer.ModelPoint);
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

        Reset();
        return ToolResult.Cancelled("Boundary fill cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Reset();
        return ToolResult.None();
    }

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return HasPreview && _previewResult?.Polyline is { } polyline
            ? new CadEntity[] { polyline }
            : Array.Empty<CadEntity>();
    }

    public ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return HasPreview && _previewResult?.Polyline is { } polyline
            ? new ToolPreviewDescriptor(
                highlightedEntities: new CadEntity[] { polyline },
                highlightedEntityKind: ToolPreviewHighlightKind.Addition)
            : ToolPreviewDescriptor.Empty;
    }

    private ToolResult UpdatePreview(
        ToolContext context,
        Point2D seedPoint)
    {
        BoundaryFillResult result = _boundaryFillService.CreateFilledPolyline(
            context.Document.GetVisibleEntities(),
            seedPoint,
            context.Creation.CurrentLayerId,
            CreateOptions(context));

        _previewResult = result.Succeeded
            ? result
            : null;

        if (!result.Succeeded)
        {
            return ToolResult.None(result.Message);
        }

        return ToolResult.Updated(BuildPreviewPrompt(result));
    }

    private ToolResult ConfirmPreview(ToolContext context)
    {
        if (_previewResult is not { Succeeded: true, Polyline: { } polyline })
        {
            return ToolResult.None("Pick inside a closed boundary before confirming Boundary Fill.");
        }

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(polyline));

        string message = BuildCompletedMessage(_previewResult);
        Reset();

        return ToolResult.Completed(message);
    }

    private static BoundaryFillOptions CreateOptions(ToolContext context)
    {
        return new BoundaryFillOptions(
            context.Coordinates.GeometryTolerance,
            gapTolerance: DefaultGapTolerance,
            includeCurveBoundaries: true);
    }

    private static string BuildPreviewPrompt(BoundaryFillResult result)
    {
        if (result.Diagnostics.BridgedGapCount > 0)
        {
            return $"Boundary found; {result.Diagnostics.BridgedGapCount} small gap(s) bridged — Enter/right-click to confirm";
        }

        if (result.Diagnostics.SampledCurveSegmentCount > 0)
        {
            return "Boundary found from sampled curve(s) — Enter/right-click to confirm";
        }

        return "Boundary found — Enter/right-click to confirm";
    }

    private static string BuildCompletedMessage(BoundaryFillResult result)
    {
        if (result.Diagnostics.BridgedGapCount > 0)
        {
            return $"Boundary fill created. Bridged {result.Diagnostics.BridgedGapCount} small gap(s).";
        }

        if (result.Diagnostics.SampledCurveSegmentCount > 0)
        {
            return "Boundary fill created from sampled curve boundary.";
        }

        return result.Message;
    }

    private void Reset()
    {
        _previewResult = null;
    }
}
