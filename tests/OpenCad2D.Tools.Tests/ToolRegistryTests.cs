using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public void Constructor_ShouldRegisterDefaultTools()
    {
        var registry = new ToolRegistry();

        Assert.True(registry.Contains(ToolId.Selection));
        Assert.True(registry.Contains(ToolId.Line));
        Assert.True(registry.Contains(ToolId.Rectangle));
        Assert.True(registry.Contains(ToolId.RectangleBySides));
        Assert.True(registry.Contains(ToolId.Circle));
        Assert.True(registry.Contains(ToolId.Arc));
        Assert.True(registry.Contains(ToolId.ArcThreePoints));
        Assert.True(registry.Contains(ToolId.Polyline));
        Assert.True(registry.Contains(ToolId.Move));
        Assert.True(registry.Contains(ToolId.Copy));
        Assert.True(registry.Contains(ToolId.Rotate));
        Assert.True(registry.Contains(ToolId.Scale));
        Assert.True(registry.Contains(ToolId.Align));
        Assert.True(registry.Contains(ToolId.BreakAtPoint));
        Assert.True(registry.Contains(ToolId.BreakBetweenPoints));
        Assert.True(registry.Contains(ToolId.Extend));
        Assert.True(registry.Contains(ToolId.Trim));
        Assert.True(registry.Contains(ToolId.Delete));
    }

    [Fact]
    public void Tools_ShouldReturnAllRegisteredDescriptors()
    {
        var registry = new ToolRegistry();

        IReadOnlyList<ToolDescriptor> tools = registry.Tools;

        Assert.Equal(18, tools.Count);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Selection);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Line);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Rectangle);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.RectangleBySides);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Move);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Copy);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Rotate);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Scale);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Align);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.BreakAtPoint);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.BreakBetweenPoints);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Extend);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Trim);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Delete);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Circle);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Arc);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.ArcThreePoints);

        Assert.Contains(
            tools,
            descriptor => descriptor.Id == ToolId.Polyline);
    }

    [Fact]
    public void GetDescriptor_ShouldReturnDescriptor()
    {
        var registry = new ToolRegistry();

        ToolDescriptor descriptor = registry.GetDescriptor(ToolId.Line);

        Assert.Equal(ToolId.Line, descriptor.Id);
        Assert.Equal("Line", descriptor.Name);
        Assert.Equal("Line", descriptor.DisplayName);
        Assert.Equal("Draw", descriptor.Category);
    }

    [Fact]
    public void GetByCategory_Draw_ShouldReturnDrawingTools()
    {
        var registry = new ToolRegistry();

        IReadOnlyList<ToolDescriptor> tools = registry.GetByCategory("Draw");

        Assert.Equal(7, tools.Count);
        Assert.Contains(tools, tool => tool.Id == ToolId.Line);
        Assert.Contains(tools, tool => tool.Id == ToolId.Rectangle);
        Assert.Contains(tools, tool => tool.Id == ToolId.RectangleBySides);
        Assert.Contains(tools, tool => tool.Id == ToolId.Circle);
        Assert.Contains(tools, tool => tool.Id == ToolId.Arc);
        Assert.Contains(tools, tool => tool.Id == ToolId.ArcThreePoints);
        Assert.Contains(tools, tool => tool.Id == ToolId.Polyline);
    }

    [Fact]
    public void GetByCategory_Modify_ShouldReturnModifyTools()
    {
        var registry = new ToolRegistry();

        IReadOnlyList<ToolDescriptor> tools = registry.GetByCategory("Modify");

        Assert.Equal(11, tools.Count);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Selection);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Move);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Copy);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Rotate);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Scale);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Align);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.BreakAtPoint);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.BreakBetweenPoints);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Extend);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Trim);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Delete);
    }

    [Fact]
    public void Create_Delete_ShouldReturnDeleteTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Delete);

        Assert.IsType<DeleteTool>(tool);
        Assert.Equal("Delete", tool.Name);
    }

    [Fact]
    public void Create_Selection_ShouldReturnSelectionTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Selection);

        Assert.IsType<SelectionTool>(tool);
        Assert.Equal("Selection", tool.Name);
    }

    [Fact]
    public void Create_Line_ShouldReturnLineTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Line);

        Assert.IsType<LineTool>(tool);
        Assert.Equal("Line", tool.Name);
    }

    [Fact]
    public void Create_Rectangle_ShouldReturnRectangleTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Rectangle);

        Assert.IsType<RectangleTool>(tool);
        Assert.Equal("Rectangle", tool.Name);
    }

    [Fact]
    public void Create_RectangleBySides_ShouldReturnRectangleBySidesTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.RectangleBySides);

        Assert.IsType<RectangleBySidesTool>(tool);
        Assert.Equal("Rectangle Sides", tool.Name);
    }

    [Fact]
    public void Create_Arc_ShouldReturnArcTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Arc);

        Assert.IsType<ArcTool>(tool);
        Assert.Equal("Arc", tool.Name);
    }

    [Fact]
    public void Create_ArcThreePoints_ShouldReturnArcThreePointsTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.ArcThreePoints);

        Assert.IsType<ArcThreePointsTool>(tool);
        Assert.Equal("Arc 3P", tool.Name);
    }

    [Fact]
    public void Create_Polyline_ShouldReturnPolylineTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Polyline);

        Assert.IsType<PolylineTool>(tool);
        Assert.Equal("Polyline", tool.Name);
    }

    [Fact]
    public void Create_Move_ShouldReturnMoveTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Move);

        Assert.IsType<MoveTool>(tool);
        Assert.Equal("Move", tool.Name);
    }


    [Fact]
    public void Create_Rotate_ShouldReturnRotateTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Rotate);

        Assert.IsType<RotateTool>(tool);
        Assert.Equal("Rotate", tool.Name);
    }

    [Fact]
    public void Create_Scale_ShouldReturnScaleTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Scale);

        Assert.IsType<ScaleTool>(tool);
        Assert.Equal("Scale", tool.Name);
    }

    [Fact]
    public void Create_Align_ShouldReturnAlignTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Align);

        Assert.IsType<AlignTool>(tool);
        Assert.Equal("Align", tool.Name);
    }

    [Fact]
    public void Create_BreakAtPoint_ShouldReturnBreakAtPointTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.BreakAtPoint);

        Assert.IsType<BreakAtPointTool>(tool);
        Assert.Equal("Break Point", tool.Name);
    }

    [Fact]
    public void Create_BreakBetweenPoints_ShouldReturnBreakBetweenPointsTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.BreakBetweenPoints);

        Assert.IsType<BreakBetweenPointsTool>(tool);
        Assert.Equal("Break Segment", tool.Name);
    }

    [Fact]
    public void Create_Extend_ShouldReturnExtendTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Extend);

        Assert.IsType<ExtendTool>(tool);
        Assert.Equal("Extend", tool.Name);
    }

    [Fact]
    public void Create_Trim_ShouldReturnTrimTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Trim);

        Assert.IsType<TrimTool>(tool);
        Assert.Equal("Trim", tool.Name);
    }

    [Fact]
    public void Create_Copy_ShouldReturnCopyTool()
    {
        var registry = new ToolRegistry();

        ICadTool tool = registry.Create(ToolId.Copy);

        Assert.IsType<CopyTool>(tool);
        Assert.Equal("Copy", tool.Name);
    }

    [Fact]
    public void Create_ShouldReturnNewToolInstanceEveryTime()
    {
        var registry = new ToolRegistry();

        ICadTool first = registry.Create(ToolId.Line);
        ICadTool second = registry.Create(ToolId.Line);

        Assert.NotSame(first, second);
        Assert.IsType<LineTool>(first);
        Assert.IsType<LineTool>(second);
    }

    [Fact]
    public void GetByCategory_ShouldBeCaseInsensitive()
    {
        var registry = new ToolRegistry();

        IReadOnlyList<ToolDescriptor> tools = registry.GetByCategory("draw");

        Assert.Equal(7, tools.Count);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Line);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Rectangle);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.RectangleBySides);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Circle);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Arc);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.ArcThreePoints);
        Assert.Contains(tools, descriptor => descriptor.Id == ToolId.Polyline);
    }

    [Fact]
    public void GetByCategory_WithEmptyCategory_ShouldThrow()
    {
        var registry = new ToolRegistry();

        Assert.Throws<ArgumentException>(
            () => registry.GetByCategory(""));
    }
}