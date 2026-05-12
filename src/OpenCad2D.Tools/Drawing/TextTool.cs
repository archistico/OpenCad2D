using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using System.Threading.Tasks;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to insert single-line text entities.
/// </summary>
public sealed class TextTool : IAsyncCadTool
{
    private readonly ITextInputProvider _textInputProvider;

    public TextTool(ITextInputProvider? textInputProvider = null)
    {
        _textInputProvider = textInputProvider ?? new DefaultTextInputProvider();
    }

    public string Name => "Text";

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

        TextInputRequest request = CreateTextInputRequest(
            context,
            insertionPoint);

        TextInputResult? input = _textInputProvider.RequestText(request);

        return CompleteTextInsertion(
            context,
            insertionPoint,
            input);
    }

    public async Task<ToolResult> OnPointerPressedAsync(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D insertionPoint = ApplySnap(
            context,
            pointer.ModelPoint);

        TextInputRequest request = CreateTextInputRequest(
            context,
            insertionPoint);

        TextInputResult? input = await _textInputProvider
            .RequestTextAsync(request)
            .ConfigureAwait(true);

        return CompleteTextInsertion(
            context,
            insertionPoint,
            input);
    }

    private TextInputRequest CreateTextInputRequest(
        ToolContext context,
        Point2D insertionPoint)
    {
        return new TextInputRequest(
            insertionPoint,
            context.Creation.CurrentTextFormatId,
            0.0,
            context.Document.TextFormats.All);
    }

    private ToolResult CompleteTextInsertion(
        ToolContext context,
        Point2D insertionPoint,
        TextInputResult? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Text))
        {
            return ToolResult.Cancelled("Text cancelled.");
        }

        var text = new TextEntity(
            insertionPoint,
            input.Text,
            input.RotationDegrees,
            input.TextFormatId,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(text));

        LastInsertionPoint = insertionPoint;
        context.CurrentBasePoint = insertionPoint;
        context.Creation.CurrentTextFormatId = input.TextFormatId;

        return ToolResult.Completed("Text created.");
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

        return ToolResult.Cancelled("Text cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastInsertionPoint = null;
        context.CurrentBasePoint = null;

        return ToolResult.None();
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
