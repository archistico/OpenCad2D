using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class FilletToolTests
{
    [Fact]
    public void RadiusOption_ShouldPromptForRadiusAndUpdateRadius()
    {
        var context = CreateContext();
        var tool = new FilletTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("R", "Radius"),
            context);
        ToolResult radiusResult = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3", 3),
            context);

        Assert.Equal(ToolResultKind.Started, optionResult.Kind);
        Assert.Equal(ToolResultKind.Started, radiusResult.Kind);
        Assert.Equal(3, tool.Radius);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
    }

    [Fact]
    public void LineLine_WithZeroRadius_ShouldJoinLinesAtIntersection()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.Count());
        Assert.All(document.Entities.All, entity => Assert.IsType<LineEntity>(entity));
    }

    [Fact]
    public void OnPointerMoved_AfterFirstLine_ShouldExposeFilletPreviewWithoutChangingDocument()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        Assert.Equal(3, preview.Count);
        Assert.Contains(preview, entity => entity is ArcEntity);
        Assert.Equal(2, document.Entities.All.Count());
    }

    [Fact]
    public void CompletedFillet_ShouldClearPreviewEntities()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Empty(tool.GetPreviewEntities());
    }

    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 5);
    }
}
