using System.Linq;
using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.Properties;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class PropertyPanelEditingTests
{

    [Fact]
    public void PropertyPanel_ForSingleSelection_ShouldShowDrawOrder()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            drawOrder: 42);
        viewModel.Workspace.Document.AddEntity(line);
        SelectEntity(viewModel, line);

        PropertyRowViewModel row = FindRow(viewModel, "Draw order");

        Assert.Equal("42", row.Value);
        Assert.False(row.IsEditable);
    }

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



    [Fact]
    public void ApplyCommand_ForLayerId_ShouldMoveEntityToExistingLayerAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var layerId = new LayerId("Annotations");

        if (!viewModel.Workspace.Document.Layers.Contains(layerId))
        {
            viewModel.Workspace.Document.Layers.Add(new Layer(layerId, "Annotations"));
        }

        var point = new PointEntity(new Point2D(10, 20));
        viewModel.Workspace.Document.AddEntity(point);
        SelectEntity(viewModel, point);

        PropertyRowViewModel row = FindRow(viewModel, "Layer id");
        row.EditableValue = "Annotations";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<PointEntity>(viewModel.Workspace.Document.Entities.GetRequired(point.Id));
        Assert.Equal(layerId, updated.LayerId);
        Assert.Equal("Entity layer updated.", viewModel.LastMessage);
        Assert.True(viewModel.Workspace.CommandHistory.CanUndo);

        viewModel.Undo();

        var restored = Assert.IsType<PointEntity>(viewModel.Workspace.Document.Entities.GetRequired(point.Id));
        Assert.Equal(point.LayerId, restored.LayerId);
    }

    [Fact]
    public void ApplyCommand_ForArcRadius_ShouldReplaceArcAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));
        viewModel.Workspace.Document.AddEntity(arc);
        SelectEntity(viewModel, arc);

        PropertyRowViewModel row = FindRow(viewModel, "Radius");
        row.EditableValue = "25";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<ArcEntity>(viewModel.Workspace.Document.Entities.GetRequired(arc.Id));
        Assert.Equal(25, updated.Radius);
        Assert.Equal("Arc updated.", viewModel.LastMessage);

        viewModel.Undo();

        var restored = Assert.IsType<ArcEntity>(viewModel.Workspace.Document.Entities.GetRequired(arc.Id));
        Assert.Equal(10, restored.Radius);
    }

    [Fact]
    public void ApplyCommand_ForTextFormat_ShouldReplaceTextFormatAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var text = new TextEntity(
            new Point2D(0, 0),
            "Label");
        viewModel.Workspace.Document.AddEntity(text);
        SelectEntity(viewModel, text);

        PropertyRowViewModel row = FindRow(viewModel, "Text format");
        row.EditableValue = "Title";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<TextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(TextFormatId.Title, updated.TextFormatId);
        Assert.Equal("Text format updated.", viewModel.LastMessage);

        viewModel.Undo();

        var restored = Assert.IsType<TextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(TextFormatId.Standard, restored.TextFormatId);
    }

    [Fact]
    public void ApplyCommand_ForPolylineClosed_ShouldUpdateClosedFlagAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            });
        viewModel.Workspace.Document.AddEntity(polyline);
        SelectEntity(viewModel, polyline);

        PropertyRowViewModel row = FindRow(viewModel, "Closed");
        row.EditableValue = "yes";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<PolylineEntity>(viewModel.Workspace.Document.Entities.GetRequired(polyline.Id));
        Assert.True(updated.IsClosed);
        Assert.Equal("Polyline updated.", viewModel.LastMessage);

        viewModel.Undo();

        var restored = Assert.IsType<PolylineEntity>(viewModel.Workspace.Document.Entities.GetRequired(polyline.Id));
        Assert.False(restored.IsClosed);
    }

    [Fact]
    public void ApplyCommand_ForDimensionTextOverride_ShouldReplaceDimensionAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal);
        viewModel.Workspace.Document.AddEntity(dimension);
        SelectEntity(viewModel, dimension);

        PropertyRowViewModel row = FindRow(viewModel, "Text override");
        row.EditableValue = "custom value";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<LinearDimensionEntity>(viewModel.Workspace.Document.Entities.GetRequired(dimension.Id));
        Assert.Equal("custom value", updated.TextOverride);
        Assert.Equal("Dimension text override updated.", viewModel.LastMessage);

        viewModel.Undo();

        var restored = Assert.IsType<LinearDimensionEntity>(viewModel.Workspace.Document.Entities.GetRequired(dimension.Id));
        Assert.Null(restored.TextOverride);
    }


    [Fact]
    public void ApplyCommand_ForMultilineTextValue_ShouldReplaceEntityAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var text = new MultilineTextEntity(
            new Point2D(0, 0),
            "First line\nSecond line");
        viewModel.Workspace.Document.AddEntity(text);
        SelectEntity(viewModel, text);

        PropertyRowViewModel row = FindRow(viewModel, "Value");
        Assert.True(row.IsEditable);

        row.EditableValue = "Updated line\nSecond updated line";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<MultilineTextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal("Updated line\nSecond updated line", updated.Text);
        Assert.Equal("Multiline text updated.", viewModel.LastMessage);
        Assert.True(viewModel.Workspace.CommandHistory.CanUndo);

        viewModel.Undo();

        var restored = Assert.IsType<MultilineTextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(text.Text, restored.Text);
    }

    [Fact]
    public void ApplyCommand_ForMultilineTextReferenceWidth_ShouldReplaceEntityAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var text = new MultilineTextEntity(
            new Point2D(0, 0),
            "Wrapped note",
            referenceWidth: 120);
        viewModel.Workspace.Document.AddEntity(text);
        SelectEntity(viewModel, text);

        PropertyRowViewModel row = FindRow(viewModel, "Reference width");
        Assert.True(row.IsEditable);

        row.EditableValue = "250.5";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<MultilineTextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(250.5, updated.ReferenceWidth);
        Assert.Equal("Multiline text reference width updated.", viewModel.LastMessage);

        viewModel.Undo();

        var restored = Assert.IsType<MultilineTextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(120, restored.ReferenceWidth);
    }

    [Fact]
    public void ApplyCommand_ForMultilineTextReferenceWidth_ShouldRejectNegativeValue()
    {
        var viewModel = new MainWindowViewModel();
        var text = new MultilineTextEntity(
            new Point2D(0, 0),
            "Wrapped note",
            referenceWidth: 120);
        viewModel.Workspace.Document.AddEntity(text);
        SelectEntity(viewModel, text);

        PropertyRowViewModel row = FindRow(viewModel, "Reference width");
        row.EditableValue = "-1";
        row.ApplyCommand.Execute(null);

        var unchanged = Assert.IsType<MultilineTextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(120, unchanged.ReferenceWidth);
        Assert.Equal("Multiline text reference width cannot be negative.", viewModel.LastMessage);
    }

    [Fact]
    public void ApplyCommand_ForMultilineTextFormat_ShouldReplaceTextFormatAndSupportUndo()
    {
        var viewModel = new MainWindowViewModel();
        var text = new MultilineTextEntity(
            new Point2D(0, 0),
            "Note");
        viewModel.Workspace.Document.AddEntity(text);
        SelectEntity(viewModel, text);

        PropertyRowViewModel row = FindRow(viewModel, "Text format");
        row.EditableValue = "Title";
        row.ApplyCommand.Execute(null);

        var updated = Assert.IsType<MultilineTextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(TextFormatId.Title, updated.TextFormatId);
        Assert.Equal("Multiline text format updated.", viewModel.LastMessage);

        viewModel.Undo();

        var restored = Assert.IsType<MultilineTextEntity>(viewModel.Workspace.Document.Entities.GetRequired(text.Id));
        Assert.Equal(TextFormatId.Standard, restored.TextFormatId);
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
