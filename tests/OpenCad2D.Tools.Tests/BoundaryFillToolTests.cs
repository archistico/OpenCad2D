using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class BoundaryFillToolTests
{
    [Fact]
    public void Constructor_ShouldExposeNameAndGridSnapMode()
    {
        var tool = new BoundaryFillTool();
        ToolContext context = CreateContext();

        Assert.Equal("Boundary Fill", tool.Name);
        Assert.Equal(SnapKind.Grid, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void ClickInsideLineRectangle_ShouldCreateFilledClosedPolyline()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Boundary fill created.", result.Message);
        Assert.Equal(5, document.Entities.Count);

        PolylineEntity polyline = Assert.Single(document.Entities.All.OfType<PolylineEntity>());

        Assert.True(polyline.IsClosed);
        Assert.True(polyline.IsFilled);
        Assert.Equal(4, polyline.Vertices.Count);
        Assert.True(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ClickInsideLineRectangle_ShouldBeUndoable()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        context.CommandHistory.Undo(document);

        Assert.Equal(4, document.Entities.Count);
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void ClickOutsideBoundary_ShouldNotCreatePolyline()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(20, 20)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("No closed boundary was found around the picked point.", result.Message);
        Assert.Equal(4, document.Entities.Count);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void CommandInput_ShouldCreateBoundaryFillFromPoint()
    {
        CadDocument document = CreateDocumentWithRectangleLines();
        ToolContext context = CreateContext(document);
        var tool = new BoundaryFillTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("5,2", new Point2D(5, 2)),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Single(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void GetPromptState_ShouldExposeBoundaryFillCommand()
    {
        var tool = new BoundaryFillTool();

        CommandPromptState prompt = tool.GetPromptState(CreateContext());

        Assert.Equal("BFILL", prompt.CommandName);
        Assert.Equal(CommandInputKind.Point, prompt.ExpectedInput);
    }

    private static CadDocument CreateDocumentWithRectangleLines()
    {
        var document = new CadDocument();

        document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        document.AddEntity(new LineEntity(new Point2D(10, 0), new Point2D(10, 5)));
        document.AddEntity(new LineEntity(new Point2D(10, 5), new Point2D(0, 5)));
        document.AddEntity(new LineEntity(new Point2D(0, 5), new Point2D(0, 0)));

        return document;
    }

    private static ToolContext CreateContext(CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
