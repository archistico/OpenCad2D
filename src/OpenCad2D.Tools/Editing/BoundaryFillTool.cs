using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;
using System.Globalization;

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

    private static readonly CommandOption GapOption = new(
        "Gap",
        "G",
        "Set the small-gap tolerance used by Boundary Fill");

    private readonly BoundaryFillService _boundaryFillService = new();
    private BoundaryFillResult? _previewResult;
    private bool _isEditingGapTolerance;
    private double _gapTolerance = DefaultGapTolerance;

    public string Name => "Boundary Fill";

    public bool HasPreview =>
        _previewResult is { Succeeded: true, Polyline: not null };

    public BoundaryFillResult? PreviewResult => _previewResult;

    public double GapTolerance => _gapTolerance;

    public bool IsEditingGapTolerance => _isEditingGapTolerance;

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.Grid;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_isEditingGapTolerance)
        {
            return new CommandPromptState(
                "BFILL",
                $"Gap tolerance <{FormatGapTolerance(_gapTolerance)}>",
                CommandInputKind.Distance,
                placeholder: "enter small-gap tolerance");
        }

        if (HasPreview)
        {
            return new CommandPromptState(
                "BFILL",
                BuildPreviewPrompt(_previewResult!),
                CommandInputKind.PointOrOption,
                new[] { GapOption },
                acceptsEmptyEnter: true,
                placeholder: "Enter/right-click to confirm, pick another point or choose Gap");
        }

        return new CommandPromptState(
            "BFILL",
            "Pick inside a closed boundary",
            CommandInputKind.PointOrOption,
            new[] { GapOption },
            placeholder: "click inside boundary, enter X/Y or choose Gap");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (_isEditingGapTolerance)
        {
            return HandleGapToleranceInput(input, context);
        }

        if (input.Kind == CommandInputSubmissionKind.Option)
        {
            return HandleOption(input.OptionKeyword);
        }

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return ConfirmPreview(context);
        }

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "BFILL expects a point inside a closed boundary or the Gap option.");
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

        if (key == CadToolKey.Enter && !_isEditingGapTolerance)
        {
            result = ConfirmPreview(context);
            return result.Changed;
        }

        if (key == CadToolKey.G && !_isEditingGapTolerance)
        {
            result = HandleOption(GapOption.Keyword);
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

        if (_isEditingGapTolerance)
        {
            return ToolResult.None("Enter a gap tolerance before picking a boundary point.");
        }

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
            return ToolResult.None(BuildFailureMessage(result));
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

    private BoundaryFillOptions CreateOptions(ToolContext context)
    {
        return new BoundaryFillOptions(
            context.Coordinates.GeometryTolerance,
            gapTolerance: _gapTolerance,
            includeCurveBoundaries: true);
    }

    private ToolResult HandleOption(string? optionKeyword)
    {
        if (GapOption.Matches(optionKeyword ?? string.Empty))
        {
            _isEditingGapTolerance = true;
            return ToolResult.Updated($"Boundary fill gap tolerance is {FormatGapTolerance(_gapTolerance)}. Enter a new tolerance.");
        }

        return ToolResult.None("Unknown Boundary Fill option. Use Gap.");
    }

    public ToolResult SetGapToleranceFromHud(
        ToolContext context,
        double gapTolerance)
    {
        ArgumentNullException.ThrowIfNull(context);

        return HandleGapToleranceInput(
            CommandInputSubmission.FromDistance(
                FormatGapTolerance(gapTolerance),
                gapTolerance),
            context);
    }

    private ToolResult HandleGapToleranceInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        double? submittedValue = input.Kind switch
        {
            CommandInputSubmissionKind.Distance => input.Distance,
            CommandInputSubmissionKind.Number => input.Number,
            _ => null
        };

        if (submittedValue is not { } gapTolerance ||
            !double.IsFinite(gapTolerance) ||
            gapTolerance <= 0.0)
        {
            return ToolResult.None(input.ErrorMessage ?? "Gap tolerance must be greater than zero.");
        }

        _gapTolerance = gapTolerance;
        _isEditingGapTolerance = false;

        if (_previewResult is { } currentPreview)
        {
            ToolResult previewResult = UpdatePreview(context, currentPreview.SeedPoint);

            return previewResult.Message is { Length: > 0 } message
                ? ToolResult.Updated($"Boundary fill gap tolerance set to {FormatGapTolerance(_gapTolerance)}. {message}")
                : ToolResult.Updated($"Boundary fill gap tolerance set to {FormatGapTolerance(_gapTolerance)}.");
        }

        return ToolResult.Updated($"Boundary fill gap tolerance set to {FormatGapTolerance(_gapTolerance)}.");
    }

    private static string BuildPreviewPrompt(BoundaryFillResult result)
    {
        string message;

        if (result.Diagnostics.BridgedGapCount > 0)
        {
            message = $"Boundary found; {result.Diagnostics.BridgedGapCount} small gap(s) bridged — Enter/right-click to confirm";
        }
        else if (result.Diagnostics.SampledCurveSegmentCount > 0)
        {
            message = "Boundary found from sampled curve(s) — Enter/right-click to confirm";
        }
        else
        {
            message = "Boundary found — Enter/right-click to confirm";
        }

        return AppendIgnoredEntityDiagnostic(message, result.Diagnostics);
    }

    private static string BuildCompletedMessage(BoundaryFillResult result)
    {
        string message;

        if (result.Diagnostics.BridgedGapCount > 0)
        {
            message = $"Boundary fill created. Bridged {result.Diagnostics.BridgedGapCount} small gap(s).";
        }
        else if (result.Diagnostics.SampledCurveSegmentCount > 0)
        {
            message = "Boundary fill created from sampled curve boundary.";
        }
        else
        {
            message = result.Message;
        }

        return AppendIgnoredEntityDiagnostic(message, result.Diagnostics);
    }

    private static string BuildFailureMessage(BoundaryFillResult result)
    {
        return AppendIgnoredEntityDiagnostic(result.Message, result.Diagnostics);
    }

    private static string AppendIgnoredEntityDiagnostic(
        string message,
        BoundaryFillDiagnostics diagnostics)
    {
        if (diagnostics.IgnoredEntityCount <= 0)
        {
            return message;
        }

        string suffix = diagnostics.IgnoredEntityCount == 1
            ? "Ignored 1 unsupported entity."
            : $"Ignored {diagnostics.IgnoredEntityCount} unsupported entities.";

        return string.IsNullOrWhiteSpace(message)
            ? suffix
            : $"{message} {suffix}";
    }

    private static string FormatGapTolerance(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void Reset()
    {
        _previewResult = null;
        _isEditingGapTolerance = false;
    }
}
