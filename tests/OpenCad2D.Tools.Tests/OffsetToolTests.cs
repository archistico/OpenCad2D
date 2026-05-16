using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class OffsetToolTests
{
    [Fact]
    public void HandleCommandInput_WithDistance_ShouldPromptForEntity()
    {
        var context = CreateContext();
        var tool = new OffsetTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromDistance("2", 2),
            context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(2, tool.Distance);
    }

    [Fact]
    public void OffsetLine_ShouldCreateParallelLineOnPickedSide()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        LineEntity offset = Assert.IsType<LineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(line.Id))));
        Assert.Equal(new Point2D(0, 2), offset.Start);
        Assert.Equal(new Point2D(10, 2), offset.End);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
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
