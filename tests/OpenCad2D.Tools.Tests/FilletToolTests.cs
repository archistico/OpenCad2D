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


    [Fact]
    public void TrimOption_ShouldPromptForTrimModeAndSetNoTrim()
    {
        var context = CreateContext();
        var tool = new FilletTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("T", "Trim"),
            context);
        ToolResult modeResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("N", "NoTrim"),
            context);

        Assert.Equal(ToolResultKind.Started, optionResult.Kind);
        Assert.Equal(ToolResultKind.Started, modeResult.Kind);
        Assert.False(tool.TrimEnabled);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
    }

    [Fact]
    public void LineLine_WithNoTrim_ShouldAddFilletArcAndKeepOriginalLines()
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
        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.All.Count());
        Assert.Contains(document.Entities.All, entity => ReferenceEquals(entity, horizontal));
        Assert.Contains(document.Entities.All, entity => ReferenceEquals(entity, vertical));
        Assert.Contains(document.Entities.All, entity => entity is ArcEntity);
    }

    [Fact]
    public void OnPointerMoved_WithNoTrim_ShouldPreviewOnlyFilletArc()
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
        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        Assert.Single(preview);
        Assert.IsType<ArcEntity>(preview[0]);
    }

    [Fact]
    public void LineLine_WithNoTrimAndZeroRadius_ShouldNotModifyDocument()
    {
        CadDocument document = new();
        var horizontal = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var vertical = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        document.AddEntity(horizontal);
        document.AddEntity(vertical);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(2, document.Entities.All.Count());
    }

    [Fact]
    public void LineLine_WithNearlyParallelLines_ShouldNotThrowOrModifyDocument()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(1000, 0));
        var second = new LineEntity(new Point2D(0, 1), new Point2D(1000, 1.000001));
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document);
        var tool = new FilletTool();

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(500, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(500, 1)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(2, document.Entities.All.Count());
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
