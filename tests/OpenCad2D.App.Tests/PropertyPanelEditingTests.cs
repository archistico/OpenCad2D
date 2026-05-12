using System.Linq;
using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.Properties;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class PropertyPanelEditingTests
{
    [Fact]
    public void ApplyCommand_ForPointX_ShouldReplaceEntityAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var point = new PointEntity(new Point2D(10, 20));
        viewModel.Workspace.Document.AddEntity(point);
        SelectEntity(viewModel, point);

        PropertyRowViewModel row = FindRow(viewModel, "X");
        row.EditableValue = "42.5";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<PointEntity>(viewModel.Workspace.Document.Entities.GetRequired(point.Id));
        Assert.Equal(new Point2D(42.5, 20), updated.Position);
        Assert.Equal("Point updated.", viewModel.LastMessage);
        Assert.True(viewModel.Workspace.CommandHistory.CanUndo);

        viewModel.Undo();

        var restored = Assert.IsType<PointEntity>(viewModel.Workspace.Document.Entities.GetRequired(point.Id));
        Assert.Equal(point.Position, restored.Position);
    }

    [Fact]
    public void ApplyCommand_ForLineEndY_ShouldReplaceEntityAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        viewModel.Workspace.Document.AddEntity(line);
        SelectEntity(viewModel, line);

        PropertyRowViewModel row = FindRow(viewModel, "End Y");
        row.EditableValue = "25";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        Assert.Equal(new Point2D(10, 25), updated.End);
        Assert.Equal("Line updated.", viewModel.LastMessage);

        viewModel.Undo();

        var restored = Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        Assert.Equal(line.End, restored.End);
    }

    [Fact]
    public void ApplyCommand_ForCircleRadius_ShouldRejectNonPositiveRadius()
    {
        var viewModel = new MainWindowViewModel();
        var circle = new CircleEntity(
            new Point2D(0, 0),
            10);
        viewModel.Workspace.Document.AddEntity(circle);
        SelectEntity(viewModel, circle);

        PropertyRowViewModel row = FindRow(viewModel, "Radius");
        row.EditableValue = "0";
        row.ApplyCommand.Execute(null);

        var unchanged = Assert.IsType<CircleEntity>(viewModel.Workspace.Document.Entities.GetRequired(circle.Id));
        Assert.Equal(10, unchanged.Radius);
        Assert.Equal("Circle radius must be greater than zero.", viewModel.LastMessage);
    }

    [Fact]
    public void ApplyCommand_ForTextValue_ShouldRejectEmptyText()
    {
        var viewModel = new MainWindowViewModel();
        var text = new TextEntity(
            new Point2D(0, 0),
            "Original");
        viewModel.Workspace.Document.AddEntity(text);
        SelectEntity(viewModel, text);

        PropertyRowViewModel row = FindRow(viewModel, "Value");
        row.EditableValue = "   ";
        row.ApplyCommand.Execute(null);

        var unchanged = Assert.IsType<TextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal("Original", unchanged.Text);
        Assert.Equal("Text value cannot be empty.", viewModel.LastMessage);
    }

    [Fact]
    public void ApplyCommand_WithInvalidNumber_ShouldKeepEntityUnchanged()
    {
        var viewModel = new MainWindowViewModel();
        var point = new PointEntity(new Point2D(10, 20));
        viewModel.Workspace.Document.AddEntity(point);
        SelectEntity(viewModel, point);

        PropertyRowViewModel row = FindRow(viewModel, "X");
        row.EditableValue = "10,5";
        row.ApplyCommand.Execute(null);

        var unchanged = Assert.IsType<PointEntity>(viewModel.Workspace.Document.Entities.GetRequired(point.Id));
        Assert.Equal(point.Position, unchanged.Position);
        Assert.Equal("Invalid numeric value. Use point as decimal separator, for example 10.5.", viewModel.LastMessage);
    }

    private static void SelectEntity(
        MainWindowViewModel viewModel,
        CadEntity entity)
    {
        viewModel.Workspace.SelectionSet.ReplaceWith(entity.Id);
        viewModel.RefreshPropertyPanel();
    }

    private static PropertyRowViewModel FindRow(
        MainWindowViewModel viewModel,
        string name)
    {
        return viewModel.PropertyPanel.Sections
            .SelectMany(section => section.Rows)
            .Single(row => row.Name == name);
    }
}
