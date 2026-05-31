using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using System;
using System.Linq;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelCommandLineTests
{
    [Theory]
    [InlineData("L", "Line")]
    [InlineData("line", "Line")]
    [InlineData("C", "Circle")]
    [InlineData("TR", "Trim")]
    [InlineData("ZW", "ZoomWindow")]
    [InlineData("HDIM", "Horizontal Dimension")]
    [InlineData("ANG", "Angular Dimension")]
    public void SubmitCommandInput_WithToolAlias_ShouldActivateTool(
        string input,
        string expectedToolName)
    {
        var viewModel = new MainWindowViewModel();

        var result = viewModel.SubmitCommandInput(input);

        Assert.Equal(expectedToolName, viewModel.ActiveToolName);
        Assert.Equal($"Tool changed to {expectedToolName}.", viewModel.LastMessage);
        Assert.Contains(input.Trim(), viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }


    [Theory]
    [InlineData("SELECTALL")]
    [InlineData("SA")]
    [InlineData("ALL")]
    public void SubmitCommandInput_WithSelectAllAction_ShouldSelectAllSelectableEntities(string input)
    {
        var viewModel = new MainWindowViewModel();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var secondLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));

        viewModel.Workspace.Document.AddEntity(firstLine);
        viewModel.Workspace.Document.AddEntity(secondLine);

        var result = viewModel.SubmitCommandInput(input);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.True(viewModel.Workspace.SelectionSet.Contains(firstLine.Id));
        Assert.True(viewModel.Workspace.SelectionSet.Contains(secondLine.Id));
        Assert.Equal("Selected 2 entities.", viewModel.LastMessage);
        Assert.Contains(input, viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("SELECTLAST")]
    [InlineData("SL")]
    [InlineData("LAST")]
    public void SubmitCommandInput_WithSelectLastAction_ShouldRestorePreviousSelection(string input)
    {
        var viewModel = new MainWindowViewModel();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var secondLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));

        viewModel.Workspace.Document.AddEntity(firstLine);
        viewModel.Workspace.Document.AddEntity(secondLine);
        viewModel.Workspace.SelectionSet.ReplaceWith(new[]
        {
            firstLine.Id,
            secondLine.Id
        });
        viewModel.Workspace.SelectionSet.Clear();

        var result = viewModel.SubmitCommandInput(input);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.True(viewModel.Workspace.SelectionSet.Contains(firstLine.Id));
        Assert.True(viewModel.Workspace.SelectionSet.Contains(secondLine.Id));
        Assert.Equal("Restored previous selection: 2 entities.", viewModel.LastMessage);
        Assert.Contains(input, viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("DESELECT")]
    [InlineData("CLEARSELECTION")]
    [InlineData("CS")]
    public void SubmitCommandInput_WithDeselectAction_ShouldClearCurrentSelection(string input)
    {
        var viewModel = new MainWindowViewModel();

        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var secondLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1));

        viewModel.Workspace.Document.AddEntity(firstLine);
        viewModel.Workspace.Document.AddEntity(secondLine);
        viewModel.Workspace.SelectionSet.ReplaceWith(new[]
        {
            firstLine.Id,
            secondLine.Id
        });

        var result = viewModel.SubmitCommandInput(input);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.Empty(viewModel.Workspace.SelectionSet.SelectedIds);
        Assert.Equal("Deselected 2 entities.", viewModel.LastMessage);
        Assert.Contains(input, viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithUnknownCommand_ShouldKeepCurrentTool()
    {
        var viewModel = new MainWindowViewModel();
        string originalToolName = viewModel.ActiveToolName;

        var result = viewModel.SubmitCommandInput("FOO");

        Assert.Equal(originalToolName, viewModel.ActiveToolName);
        Assert.Equal("Unknown command or alias 'FOO'.", viewModel.LastMessage);
        Assert.Empty(viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithCoordinate_ShouldKeepExistingPointInputBehavior()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.SubmitCommandInput("L");

        var result = viewModel.SubmitCommandInput("0,0");

        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.Contains("L", viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithAbsoluteCoordinatesForLine_ShouldCreateLine()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("100,50");
        var result = viewModel.SubmitCommandInput("150,50");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Line created.", viewModel.LastMessage);
        Assert.Contains(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>(),
            line => line.Start == new Point2D(100, 50) &&
                    line.End == new Point2D(150, 50));
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithAbsoluteCoordinatesForPoint_ShouldCreatePoint()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("POINT");
        var result = viewModel.SubmitCommandInput("-10.5,20.25");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Point created.", viewModel.LastMessage);
        Assert.Contains(
            viewModel.Workspace.Document.Entities.All.OfType<PointEntity>(),
            point => point.Position == new Point2D(-10.5, 20.25));
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithAbsoluteCoordinatesForCircle_ShouldCreateCircle()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("C");
        viewModel.SubmitCommandInput("10,10");
        var result = viewModel.SubmitCommandInput("13,14");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Circle created.", viewModel.LastMessage);
        Assert.Contains(
            viewModel.Workspace.Document.Entities.All.OfType<CircleEntity>(),
            circle => circle.Center == new Point2D(10, 10) &&
                      circle.Radius == 5);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithAbsoluteCoordinates_ShouldNotAddPointInputsToCommandHistory()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("10,0");

        Assert.Single(viewModel.CommandLineHistory);
        Assert.Equal("L", viewModel.CommandLineHistory[0]);
    }

    [Fact]
    public void SubmitCommandInput_WithInvalidAbsoluteCoordinate_ShouldReportClearError()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        var result = viewModel.SubmitCommandInput("10,");

        Assert.Equal(
            "Invalid absolute coordinate format. Use x,y for example: 100,50.",
            viewModel.LastMessage);
        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }



    [Fact]
    public void SubmitCommandInput_WithRelativeCoordinatesForLine_ShouldCreateLineFromBasePoint()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("10,10");
        var result = viewModel.SubmitCommandInput("@100,-25");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Line created.", viewModel.LastMessage);
        Assert.Contains(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>(),
            line => line.Start == new Point2D(10, 10) &&
                    line.End == new Point2D(110, -15));
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithRelativeCoordinatesAndNoBasePoint_ShouldReportClearError()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        var result = viewModel.SubmitCommandInput("@100,0");

        Assert.Equal("Relative coordinate input requires a reference point.", viewModel.LastMessage);
        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithDirectDistanceForLine_ShouldUseCurrentCursorDirection()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(10, 0));
        var result = viewModel.SubmitCommandInput("5");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Line created.", viewModel.LastMessage);
        Assert.Contains(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>(),
            line => line.Start == new Point2D(0, 0) &&
                    line.End == new Point2D(5, 0));
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithDirectDistanceAndNoBasePoint_ShouldReportClearError()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        var result = viewModel.SubmitCommandInput("5");

        Assert.Equal("Direct distance requires a base point.", viewModel.LastMessage);
        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithDirectDistanceAndNoCursorDirection_ShouldReportClearError()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(0, 0));
        var result = viewModel.SubmitCommandInput("5");

        Assert.Equal("Move the cursor to indicate a direction.", viewModel.LastMessage);
        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithRelativeCoordinatesAndDistance_ShouldNotAddPointInputsToCommandHistory()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("@10,0");

        Assert.Single(viewModel.CommandLineHistory);
        Assert.Equal("L", viewModel.CommandLineHistory[0]);
    }

    [Fact]
    public void SubmitCommandInput_WithEmptyInput_ShouldReportClearError()
    {
        var viewModel = new MainWindowViewModel();

        var result = viewModel.SubmitCommandInput(" ");

        Assert.Equal("No command to repeat.", viewModel.LastMessage);
        Assert.Empty(viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }
    [Theory]
    [InlineData("100<0", 100, 0)]
    [InlineData("100<90", 0, 100)]
    [InlineData("100<180", -100, 0)]
    [InlineData("100<270", 0, -100)]
    [InlineData("100<-90", 0, -100)]
    [InlineData("100<450", 0, 100)]
    public void SubmitCommandInput_WithDistanceAngleForLine_ShouldCreateLineAtCadAngle(
        string input,
        double expectedEndX,
        double expectedEndY)
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        var result = viewModel.SubmitCommandInput(input);

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Line created.", viewModel.LastMessage);
        Assert.Contains(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>(),
            line => line.Start == Point2D.Origin &&
                    ArePointsNear(new Point2D(expectedEndX, expectedEndY), line.End));
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithDistanceAngleAndNoBasePoint_ShouldReportClearError()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        var result = viewModel.SubmitCommandInput("100<45");

        Assert.Equal("Polar coordinate input requires a reference point.", viewModel.LastMessage);
        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithDistanceAngle_ShouldNotAddPointInputToCommandHistory()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("100<0");

        Assert.Single(viewModel.CommandLineHistory);
        Assert.Equal("L", viewModel.CommandLineHistory[0]);
    }


    [Fact]
    public void SubmitCommandInput_WithEmptyInputDuringRequiredCommandStep_ShouldKeepActiveCommand()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("C");
        var result = viewModel.SubmitCommandInput(" ");

        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Equal("Input is required for the current command step.", viewModel.LastMessage);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.Equal("L", viewModel.CommandLineHistory[0]);
        Assert.True(viewModel.CanRepeatLastCommand);
        Assert.Equal("L", viewModel.LastCommandText);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithEmptyInputAndNoLastCommand_ShouldReportClearError()
    {
        var viewModel = new MainWindowViewModel();

        var result = viewModel.SubmitCommandInput(" ");

        Assert.Equal("No command to repeat.", viewModel.LastMessage);
        Assert.Empty(viewModel.CommandLineHistory);
        Assert.False(viewModel.CanRepeatLastCommand);
        Assert.Equal(string.Empty, viewModel.LastCommandText);
        Assert.NotNull(result);
    }

    [Fact]
    public void RepeatLastCommand_AfterCoordinateInput_ShouldRepeatLastToolCommandOnly()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("10,0");
        var result = viewModel.RepeatLastCommand();

        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Equal("Repeated command: Line.", viewModel.LastMessage);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.Equal("L", viewModel.CommandLineHistory[0]);
        Assert.Equal("L", viewModel.LastCommandText);
        Assert.NotNull(result);
    }

    [Fact]
    public void SetTool_ShouldRegisterLastCommandForRepeatWithoutAddingHistory()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetTool(OpenCad2D.Tools.Common.ToolId.Circle);
        viewModel.SetTool(OpenCad2D.Tools.Common.ToolId.Line);
        var result = viewModel.RepeatLastCommand();

        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Equal("Repeated command: Line.", viewModel.LastMessage);
        Assert.Empty(viewModel.CommandLineHistory);
        Assert.True(viewModel.CanRepeatLastCommand);
        Assert.Equal("Line", viewModel.LastCommandText);
        Assert.NotNull(result);
    }



    [Fact]
    public void SubmitCommandInput_WithInvalidTextDuringActiveCommand_ShouldNotReplaceRepeatableCommand()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("FOO");
        var result = viewModel.SubmitCommandInput(string.Empty);

        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Equal("Input is required for the current command step.", viewModel.LastMessage);
        Assert.Single(viewModel.CommandLineHistory);
        Assert.Equal("L", viewModel.CommandLineHistory[0]);
        Assert.Equal("L", viewModel.LastCommandText);
        Assert.NotNull(result);
    }

    [Fact]
    public void RepeatLastCommandFromCanvas_ShouldNotInterruptActivePointCommand()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        var result = viewModel.RepeatLastCommandFromCanvas();

        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Equal("Finish or cancel the current command before repeating the last command.", viewModel.LastMessage);
        Assert.Equal("L", viewModel.LastCommandText);
        Assert.NotNull(result);
    }

    [Fact]
    public void RepeatLastCommandFromCanvas_WhenIdle_ShouldRepeatLastToolCommand()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("C");
        var result = viewModel.RepeatLastCommandFromCanvas();

        Assert.Equal("Circle", viewModel.ActiveToolName);
        Assert.Equal("Repeated command: Circle.", viewModel.LastMessage);
        Assert.Equal("C", viewModel.LastCommandText);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithToolAlias_ShouldAppendVisibleCommandHistory()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");

        Assert.Contains("> L", viewModel.VisibleCommandHistory);
        Assert.Contains("Command: Line", viewModel.VisibleCommandHistory);
        Assert.Contains(viewModel.CommandPromptText, viewModel.VisibleCommandHistory);
    }

    [Fact]
    public void SubmitCommandInput_WithEmptyInputDuringRequiredCommandStep_ShouldAppendEnterToVisibleCommandHistory()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("C");
        viewModel.SubmitCommandInput(string.Empty);

        Assert.Contains("> Enter", viewModel.VisibleCommandHistory);
        Assert.Contains("Input is required for the current command step.", viewModel.VisibleCommandHistory);
    }

    [Fact]
    public void VisibleCommandHistory_ShouldKeepMostRecentEntriesOnly()
    {
        var viewModel = new MainWindowViewModel();

        for (int i = 0; i < 12; i++)
        {
            viewModel.SubmitCommandInput("UNKNOWN" + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.True(viewModel.VisibleCommandHistory.Count <= 8);
        Assert.Contains("> UNKNOWN11", viewModel.VisibleCommandHistory);
    }

    [Fact]
    public void SubmitCommandInput_WithRelativePolarCoordinatesForLine_ShouldCreateLineFromBasePoint()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("10,20");
        var result = viewModel.SubmitCommandInput("@100<0");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Line created.", viewModel.LastMessage);
        Assert.Contains(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>(),
            line => line.Start == new Point2D(10, 20) &&
                    ArePointsNear(new Point2D(110, 20), line.End));
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WhenLineIsWaitingForSecondPoint_ShouldRouteTextToActiveCommand()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        var result = viewModel.SubmitCommandInput("C");

        Assert.Equal("Line", viewModel.ActiveToolName);
        Assert.Equal("Invalid point format. Use x,y, @x,y or @distance<angle.", viewModel.LastMessage);
        Assert.DoesNotContain("C", viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WhenLineIsActive_ShouldShowCommandDrivenPrompt()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");

        Assert.Equal("LINE: Specify first point:", viewModel.CommandPromptText);
    }

    [Fact]
    public void SubmitCommandInput_WithPolylineCoordinatesAndEnter_ShouldCreateOpenPolyline()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("PL");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("@10,0");
        var result = viewModel.SubmitCommandInput(string.Empty);

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Polyline created.", viewModel.LastMessage);
        PolylineEntity polyline = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.False(polyline.IsClosed);
        Assert.Equal(new[] { new Point2D(0, 0), new Point2D(10, 0) }, polyline.Vertices);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithPolylineCloseOption_ShouldCreateClosedPolyline()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("@10,0");
        viewModel.SubmitCommandInput("@0,10");
        var result = viewModel.SubmitCommandInput("C");

        Assert.Equal("Polyline", viewModel.ActiveToolName);
        Assert.Equal("Closed polyline created.", viewModel.LastMessage);
        PolylineEntity polyline = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
        Assert.DoesNotContain("C", viewModel.CommandLineHistory);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithPolylineUndoOption_ShouldRemoveLastVertex()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("@10,0");
        var result = viewModel.SubmitCommandInput("U");

        Assert.Equal("Polyline", viewModel.ActiveToolName);
        Assert.Equal("Polyline line mode: specify next point, press Enter/right-click to finish, C to close, or A for arc.", viewModel.LastMessage);
        Assert.DoesNotContain("U", viewModel.CommandLineHistory);
        Assert.Equal(0, viewModel.Workspace.Document.Entities.Count);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WhenPolylineIsActive_ShouldShowCommandDrivenPrompt()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");

        Assert.Equal("POLYLINE: Specify first point:", viewModel.CommandPromptText);

        viewModel.SubmitCommandInput("0,0");

        Assert.Equal("POLYLINE LINE: Specify next point or [Arc/Close/Undo]:", viewModel.CommandPromptText);
    }




    [Fact]
    public void SubmitCommandInput_WithCircleCoordinates_ShouldCreateCircle()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("C");
        viewModel.SubmitCommandInput("0,0");
        var result = viewModel.SubmitCommandInput("@10,0");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Circle created.", viewModel.LastMessage);
        CircleEntity circle = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<CircleEntity>());
        Assert.Equal(new Point2D(0, 0), circle.Center);
        Assert.Equal(10, circle.Radius);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithRectangleCoordinates_ShouldCreateRectangle()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("REC");
        viewModel.SubmitCommandInput("1,2");
        var result = viewModel.SubmitCommandInput("@9,18");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Rectangle created.", viewModel.LastMessage);
        PolylineEntity rectangle = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(rectangle.IsClosed);
        Assert.Equal(new Point2D(1, 2), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(10, 20), rectangle.Vertices[2]);
        Assert.NotNull(result);
    }

    [Fact]
    public void SubmitCommandInput_WithArc3PCoordinates_ShouldCreateArc()
    {
        var viewModel = new MainWindowViewModel();
        int initialCount = viewModel.Workspace.Document.Entities.Count;

        viewModel.SubmitCommandInput("A3P");
        viewModel.SubmitCommandInput("10,0");
        viewModel.SubmitCommandInput("0,10");
        var result = viewModel.SubmitCommandInput("-10,0");

        Assert.Equal(initialCount + 1, viewModel.Workspace.Document.Entities.Count);
        Assert.Equal("Arc 3P created.", viewModel.LastMessage);
        ArcEntity arc = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(new Point2D(0, 0), arc.Center);
        Assert.Equal(10, arc.Radius, precision: 10);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("C", "CIRCLE: Specify center point:")]
    [InlineData("REC", "RECTANGLE: Specify first corner:")]
    [InlineData("A3P", "ARC3P: Specify start point:")]
    public void SubmitCommandInput_WhenBaseDrawToolIsActive_ShouldShowCommandDrivenPrompt(
        string command,
        string expectedPrompt)
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput(command);

        Assert.Equal(expectedPrompt, viewModel.CommandPromptText);
    }


    [Fact]
    public void NavigateCommandHistoryPrevious_ShouldReturnMostRecentCommandFirst()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.Escape();
        viewModel.SubmitCommandInput("C");
        viewModel.Escape();
        viewModel.SubmitCommandInput("REC");

        Assert.Equal("REC", viewModel.NavigateCommandHistoryPrevious());
        Assert.Equal("C", viewModel.NavigateCommandHistoryPrevious());
        Assert.Equal("L", viewModel.NavigateCommandHistoryPrevious());
        Assert.Equal("L", viewModel.NavigateCommandHistoryPrevious());
    }

    [Fact]
    public void NavigateCommandHistoryNext_ShouldReturnNewerCommandsAndThenEmptyInput()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.Escape();
        viewModel.SubmitCommandInput("C");

        Assert.Equal("C", viewModel.NavigateCommandHistoryPrevious());
        Assert.Equal("L", viewModel.NavigateCommandHistoryPrevious());
        Assert.Equal("C", viewModel.NavigateCommandHistoryNext());
        Assert.Equal(string.Empty, viewModel.NavigateCommandHistoryNext());
        Assert.Equal(string.Empty, viewModel.NavigateCommandHistoryNext());
    }

    [Fact]
    public void SubmitCommandInput_ShouldResetCommandHistoryNavigation()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.Escape();
        viewModel.SubmitCommandInput("C");

        Assert.Equal("C", viewModel.NavigateCommandHistoryPrevious());

        viewModel.Escape();
        viewModel.SubmitCommandInput("REC");

        Assert.Equal("REC", viewModel.NavigateCommandHistoryPrevious());
    }

    [Fact]
    public void NavigateCommandHistoryPrevious_WhenHistoryIsEmpty_ShouldReturnEmptyInput()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Equal(string.Empty, viewModel.NavigateCommandHistoryPrevious());
        Assert.Equal(string.Empty, viewModel.NavigateCommandHistoryNext());
    }


    [Theory]
    [InlineData("li", "LINE")]
    [InlineData("mt", "MTEXT")]
    [InlineData("m", "MOVE")]
    [InlineData("selecta", "SELECTALL")]
    [InlineData("distributeh", "DISTRIBUTEHORIZONTAL")]
    public void GetCommandAutocompleteSuggestion_ShouldCompleteKnownCommands(
        string input,
        string expectedSuggestion)
    {
        var viewModel = new MainWindowViewModel();

        string? suggestion = viewModel.GetCommandAutocompleteSuggestion(input);

        Assert.Equal(expectedSuggestion, suggestion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("10,20")]
    [InlineData("unknowncommand")]
    [InlineData("LINE")]
    public void GetCommandAutocompleteSuggestion_WhenNoCompletionExists_ShouldReturnNull(string input)
    {
        var viewModel = new MainWindowViewModel();

        Assert.Null(viewModel.GetCommandAutocompleteSuggestion(input));
    }


    [Fact]
    public void CommandHudInput_LineDistance_ShouldFreezeVisibleLiveAngle()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(0, 10));

        bool distanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "5",
            confirm: false,
            out _);

        viewModel.SetMousePosition(new Point2D(10, 0));

        bool confirmed = viewModel.TryConfirmCommandHudInputOverrides(out var result);

        Assert.True(distanceHandled);
        Assert.True(confirmed);
        Assert.NotNull(result);

        LineEntity line = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>());
        Assert.True(ArePointsNear(new Point2D(0, 0), line.Start));
        Assert.True(ArePointsNear(new Point2D(0, 5), line.End));
    }

    [Fact]
    public void CommandHudInput_PolylineFirstPoint_ShouldAcceptAbsoluteXAndY()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");

        bool xHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _);
        bool yHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "20",
            confirm: true,
            out _);

        viewModel.SubmitCommandInput("@5,0");
        var result = viewModel.SubmitCommandInput(string.Empty);

        Assert.True(xHandled);
        Assert.True(yHandled);
        Assert.NotNull(result);

        PolylineEntity polyline = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(2, polyline.Vertices.Count);
        Assert.True(ArePointsNear(new Point2D(10, 20), polyline.Vertices[0]));
        Assert.True(ArePointsNear(new Point2D(15, 20), polyline.Vertices[1]));
    }

    [Fact]
    public void CommandHudInput_PolylineDistanceAngle_ShouldCreateNextVertexAndResetOverride()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(0, 10));

        bool distanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "5",
            confirm: false,
            out _);
        bool angleHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "0",
            confirm: true,
            out _);

        viewModel.SetMousePosition(new Point2D(20, 0));
        CommandHudFieldViewModel distanceField = Assert.Single(
            viewModel.CommandHudState.Fields,
            field => field.Kind == CommandHudFieldKind.Distance);

        var result = viewModel.SubmitCommandInput(string.Empty);

        Assert.True(distanceHandled);
        Assert.True(angleHandled);
        Assert.True(distanceField.LiveValue is > 5.0);
        Assert.NotNull(result);

        PolylineEntity polyline = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(2, polyline.Vertices.Count);
        Assert.True(ArePointsNear(new Point2D(0, 0), polyline.Vertices[0]));
        Assert.True(ArePointsNear(new Point2D(5, 0), polyline.Vertices[1]));
    }


    [Fact]
    public void CommandHudInput_LineFirstPoint_ShouldAcceptAbsoluteXAndY()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");

        bool xHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "3",
            confirm: false,
            out _);
        bool yHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "4",
            confirm: true,
            out _);

        viewModel.SetMousePosition(new Point2D(13, 4));
        bool distanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "10",
            confirm: true,
            out _);

        Assert.True(xHandled);
        Assert.True(yHandled);
        Assert.True(distanceHandled);

        LineEntity line = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>());
        Assert.True(ArePointsNear(new Point2D(3, 4), line.Start));
        Assert.True(ArePointsNear(new Point2D(13, 4), line.End));
    }

    [Fact]
    public void CommandHudInput_LineDistanceAngle_ShouldCreateExpectedSegment()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(0, 10));

        bool distanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "10",
            confirm: false,
            out _);
        bool angleHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "45",
            confirm: true,
            out _);

        Assert.True(distanceHandled);
        Assert.True(angleHandled);

        LineEntity line = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>());
        Assert.True(ArePointsNear(new Point2D(0, 0), line.Start));
        Assert.True(ArePointsNear(
            new Point2D(10.0 / Math.Sqrt(2.0), 10.0 / Math.Sqrt(2.0)),
            line.End));
    }

    [Fact]
    public void CommandHudInput_PolylineDistanceOnly_ShouldFreezeVisibleLiveAngle()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(0, 10));

        bool distanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "5",
            confirm: false,
            out _);

        viewModel.SetMousePosition(new Point2D(10, 0));

        bool confirmed = viewModel.TryConfirmCommandHudInputOverrides(out _);
        var result = viewModel.SubmitCommandInput(string.Empty);

        Assert.True(distanceHandled);
        Assert.True(confirmed);
        Assert.NotNull(result);

        PolylineEntity polyline = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(2, polyline.Vertices.Count);
        Assert.True(ArePointsNear(new Point2D(0, 0), polyline.Vertices[0]));
        Assert.True(ArePointsNear(new Point2D(0, 5), polyline.Vertices[1]));
    }

    [Fact]
    public void CommandHudInput_PolylineMultipleSegments_ShouldNotReusePreviousOverrides()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(10, 0));

        bool firstDistanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "5",
            confirm: true,
            out _);

        viewModel.SetMousePosition(new Point2D(5, 10));

        bool secondDistanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "10",
            confirm: true,
            out _);

        var result = viewModel.SubmitCommandInput(string.Empty);

        Assert.True(firstDistanceHandled);
        Assert.True(secondDistanceHandled);
        Assert.NotNull(result);

        PolylineEntity polyline = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(3, polyline.Vertices.Count);
        Assert.True(ArePointsNear(new Point2D(0, 0), polyline.Vertices[0]));
        Assert.True(ArePointsNear(new Point2D(5, 0), polyline.Vertices[1]));
        Assert.True(ArePointsNear(new Point2D(5, 10), polyline.Vertices[2]));
    }

    [Fact]
    public void CommandHudInput_PolylineFirstPointIncompleteCoordinates_ShouldNotCreatePoint()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("PL");

        bool xHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: true,
            out var result);

        Assert.True(xHandled);
        Assert.NotNull(result);
        Assert.DoesNotContain(
            viewModel.Workspace.Document.Entities.All,
            entity => entity is PolylineEntity);
        Assert.Contains("both X and Y", viewModel.LastMessage);
    }


    [Fact]
    public void CommandHudInput_MoveDistanceAngle_ShouldMoveSelectedEntity()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("M");

        bool baseXHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _);
        bool baseYHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _);

        viewModel.SetMousePosition(new Point2D(0, 10));

        bool distanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "5",
            confirm: false,
            out _);
        bool angleHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "90",
            confirm: true,
            out _);

        Assert.True(baseXHandled);
        Assert.True(baseYHandled);
        Assert.True(distanceHandled);
        Assert.True(angleHandled);

        LineEntity moved = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        Assert.True(ArePointsNear(new Point2D(0, 5), moved.Start));
        Assert.True(ArePointsNear(new Point2D(10, 5), moved.End));
    }

    [Fact]
    public void CommandHudInput_CopyDistanceAngle_ShouldCopySelectedEntity()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("COPY");

        bool baseXHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _);
        bool baseYHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _);

        viewModel.SetMousePosition(new Point2D(10, 0));

        bool distanceHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "5",
            confirm: false,
            out _);
        bool angleHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "0",
            confirm: true,
            out _);

        Assert.True(baseXHandled);
        Assert.True(baseYHandled);
        Assert.True(distanceHandled);
        Assert.True(angleHandled);

        LineEntity[] lines = viewModel.Workspace.Document.Entities.All
            .OfType<LineEntity>()
            .ToArray();

        Assert.Equal(2, lines.Length);
        Assert.Contains(lines, entity =>
            ArePointsNear(new Point2D(0, 0), entity.Start) &&
            ArePointsNear(new Point2D(10, 0), entity.End));
        Assert.Contains(lines, entity =>
            ArePointsNear(new Point2D(5, 0), entity.Start) &&
            ArePointsNear(new Point2D(15, 0), entity.End));
    }

    [Fact]
    public void CommandHudInput_MoveDestination_ShouldExposeEditableDistanceAngleAndCoordinates()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("M");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(10, 0));

        CommandHudFieldKind[] editableKinds = viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();

        Assert.Contains(CommandHudFieldKind.Distance, editableKinds);
        Assert.Contains(CommandHudFieldKind.Angle, editableKinds);
        Assert.Contains(CommandHudFieldKind.X, editableKinds);
        Assert.Contains(CommandHudFieldKind.Y, editableKinds);
    }


    [Fact]
    public void CommandHudInput_RotateAngle_ShouldRotateSelectedEntity()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(1, 0),
            new Point2D(2, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("RO");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "1",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        bool angleHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "90",
            confirm: true,
            out _);

        Assert.True(angleHandled);

        LineEntity rotated = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        Assert.True(ArePointsNear(new Point2D(0, 1), rotated.Start));
        Assert.True(ArePointsNear(new Point2D(0, 2), rotated.End));
    }

    [Fact]
    public void CommandHudInput_ScaleFactor_ShouldScaleSelectedEntity()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(1, 0),
            new Point2D(2, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("SC");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "1",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        bool factorHandled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Factor,
            "2",
            confirm: true,
            out _);

        Assert.True(factorHandled);

        LineEntity scaled = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        Assert.True(ArePointsNear(new Point2D(2, 0), scaled.Start));
        Assert.True(ArePointsNear(new Point2D(4, 0), scaled.End));
    }

    [Fact]
    public void CommandHudInput_Align_ShouldAcceptCoordinateFieldsForPointPhases()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("ALIGN");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "10",
            confirm: true,
            out _));

        viewModel.SubmitCommandInput("N");

        LineEntity aligned = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        Assert.True(ArePointsNear(new Point2D(0, 0), aligned.Start));
        Assert.True(ArePointsNear(new Point2D(0, 10), aligned.End));
    }



    [Fact]
    public void CommandHudInput_Mirror_ShouldExposeCoordinateThenDistanceAngleFields()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("MIRROR");

        CommandHudFieldKind[] firstPointKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.X, firstPointKinds);
        Assert.Contains(CommandHudFieldKind.Y, firstPointKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Distance, firstPointKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Angle, firstPointKinds);

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "0",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        viewModel.SetMousePosition(new Point2D(0, 10));

        CommandHudFieldKind[] secondPointKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.Distance, secondPointKinds);
        Assert.Contains(CommandHudFieldKind.Angle, secondPointKinds);
        Assert.Contains(CommandHudFieldKind.X, secondPointKinds);
        Assert.Contains(CommandHudFieldKind.Y, secondPointKinds);
    }

    [Fact]
    public void CommandHudInput_OffsetDistance_ShouldBeEditableAndAccepted()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("OFFSET");

        CommandHudFieldKind[] editableKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.Distance, editableKinds);

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "5",
            confirm: true,
            out var result);

        Assert.True(handled);
        Assert.NotNull(result);
        Assert.Contains("Select object", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_FilletRadius_ShouldBeEditableAndAccepted()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("FILLET");
        viewModel.SubmitCommandInput("R");

        CommandHudFieldKind[] editableKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.Radius, editableKinds);

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Radius,
            "2.5",
            confirm: true,
            out var result);

        Assert.True(handled);
        Assert.NotNull(result);
        Assert.Contains("Fillet radius set", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_ChamferDistance_ShouldBeEditableAndAccepted()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("CHAMFER");
        viewModel.SubmitCommandInput("D");

        CommandHudFieldKind[] editableKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.Distance, editableKinds);

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "3",
            confirm: true,
            out var result);

        Assert.True(handled);
        Assert.NotNull(result);
        Assert.Contains("Chamfer distance set", viewModel.LastMessage);
    }


    [Fact]
    public void CommandHudInput_FilletRadiusZero_ShouldBeAccepted()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("FILLET");
        viewModel.SubmitCommandInput("R");

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Radius,
            "0",
            confirm: true,
            out var result);

        Assert.True(handled);
        Assert.NotNull(result);
        Assert.Contains("Fillet radius set to 0", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_ChamferDistanceZero_ShouldBeAccepted()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("CHAMFER");
        viewModel.SubmitCommandInput("D");

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "0",
            confirm: true,
            out var result);

        Assert.True(handled);
        Assert.NotNull(result);
        Assert.Contains("Chamfer distance set to 0", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_OffsetDistanceZero_ShouldBeRejected()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("OFFSET");

        bool handled = viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "0",
            confirm: true,
            out var result);

        Assert.True(handled);
        Assert.NotNull(result);
        Assert.Contains("Distance must be greater than zero", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_BreakAtPoint_ShouldAcceptCoordinateFields()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);

        viewModel.SubmitCommandInput("BREAKPOINT");
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0)));

        CommandHudFieldKind[] editableKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.X, editableKinds);
        Assert.Contains(CommandHudFieldKind.Y, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Distance, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Angle, editableKinds);

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "5",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out var result));

        Assert.NotNull(result);
        Assert.Equal("Entity broken at point.", viewModel.LastMessage);

        LineEntity[] lines = viewModel.Workspace.Document.Entities.All
            .OfType<LineEntity>()
            .ToArray();

        Assert.Equal(2, lines.Length);
        Assert.Contains(lines, segment =>
            ArePointsNear(new Point2D(0, 0), segment.Start) &&
            ArePointsNear(new Point2D(5, 0), segment.End));
        Assert.Contains(lines, segment =>
            ArePointsNear(new Point2D(5, 0), segment.Start) &&
            ArePointsNear(new Point2D(10, 0), segment.End));
    }

    [Fact]
    public void CommandHudInput_BreakSegment_ShouldAcceptCoordinateThenDistanceAngleFields()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);

        viewModel.SubmitCommandInput("BREAK");
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(1, 0)));

        CommandHudFieldKind[] firstPointKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.X, firstPointKinds);
        Assert.Contains(CommandHudFieldKind.Y, firstPointKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Distance, firstPointKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Angle, firstPointKinds);

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "2",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "0",
            confirm: true,
            out _));

        viewModel.SetMousePosition(new Point2D(6, 0));

        CommandHudFieldKind[] secondPointKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.Distance, secondPointKinds);
        Assert.Contains(CommandHudFieldKind.Angle, secondPointKinds);
        Assert.Contains(CommandHudFieldKind.X, secondPointKinds);
        Assert.Contains(CommandHudFieldKind.Y, secondPointKinds);

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "4",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "0",
            confirm: true,
            out var result));

        Assert.NotNull(result);
        Assert.Equal("Entity segment removed.", viewModel.LastMessage);

        LineEntity[] lines = viewModel.Workspace.Document.Entities.All
            .OfType<LineEntity>()
            .ToArray();

        Assert.Equal(2, lines.Length);
        Assert.Contains(lines, segment =>
            ArePointsNear(new Point2D(0, 0), segment.Start) &&
            ArePointsNear(new Point2D(2, 0), segment.End));
        Assert.Contains(lines, segment =>
            ArePointsNear(new Point2D(6, 0), segment.Start) &&
            ArePointsNear(new Point2D(10, 0), segment.End));
    }

    [Fact]
    public void CommandHudInput_BoundaryFillSeedPoint_ShouldExposeCoordinateFields()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("BOUNDARYFILL");

        CommandHudFieldKind[] editableKinds = GetEditableHudFieldKinds(viewModel);
        Assert.Contains(CommandHudFieldKind.X, editableKinds);
        Assert.Contains(CommandHudFieldKind.Y, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Distance, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Angle, editableKinds);
    }

    [Fact]
    public void CommandHudInput_BoundaryFillSeedPoint_ShouldCreateFillFromCoordinates()
    {
        var viewModel = new MainWindowViewModel();
        AddRectangleBoundary(viewModel);

        viewModel.SubmitCommandInput("BOUNDARYFILL");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "5",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "2",
            confirm: true,
            out var result));

        Assert.NotNull(result);
        Assert.Equal("Boundary fill created.", viewModel.LastMessage);
        Assert.Equal(5, viewModel.Workspace.Document.Entities.Count);

        PolylineEntity fill = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(fill.IsClosed);
        Assert.True(fill.IsFilled);
    }

    [Fact]
    public void CommandHudInput_BoundaryFillSeedPointOutsideBoundary_ShouldNotCreateFill()
    {
        var viewModel = new MainWindowViewModel();
        AddRectangleBoundary(viewModel);

        viewModel.SubmitCommandInput("BOUNDARYFILL");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "20",
            confirm: false,
            out _));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Y,
            "20",
            confirm: true,
            out var result));

        Assert.NotNull(result);
        Assert.Equal("No closed boundary was found around the picked point.", viewModel.LastMessage);
        Assert.Equal(4, viewModel.Workspace.Document.Entities.Count);
        Assert.Empty(viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
    }

    [Theory]
    [InlineData("TRIM")]
    [InlineData("EXTEND")]
    [InlineData("DELETE")]
    [InlineData("EXPLODE")]
    [InlineData("JOIN")]
    public void CommandHudInput_SelectionOnlyModifyTools_ShouldNotExposeNumericOverrides(string command)
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput(command);

        CommandHudFieldKind[] editableKinds = GetEditableHudFieldKinds(viewModel);
        Assert.DoesNotContain(CommandHudFieldKind.Distance, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Angle, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Width, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Height, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Radius, editableKinds);
        Assert.DoesNotContain(CommandHudFieldKind.Factor, editableKinds);
    }

    [Theory]
    [InlineData("TRIM")]
    [InlineData("EXTEND")]
    [InlineData("DELETE")]
    [InlineData("EXPLODE")]
    [InlineData("JOIN")]
    public void CommandHudInput_SelectionOnlyModifyTools_ShouldCancelToSelectionWithEscape(string command)
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput(command);

        Assert.True(viewModel.IsCommandHudVisible);

        ToolResult result = viewModel.Escape();

        Assert.NotNull(result);
        Assert.Equal("Selection", viewModel.ActiveToolName);
        Assert.False(viewModel.IsCommandHudVisible);
        Assert.Contains("Selection tool active", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_EscapeWithActiveCoordinateOverride_ShouldCancelToSelection()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SubmitCommandInput("L");
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _));

        ToolResult result = viewModel.Escape();

        Assert.NotNull(result);
        Assert.Equal("Selection", viewModel.ActiveToolName);
        Assert.False(viewModel.IsCommandHudVisible);
        Assert.Contains("Line command cancelled", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_Delete_ShouldSelectByPointerAndConfirmWithEnter()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(line);

        viewModel.SubmitCommandInput("DELETE");
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0)));

        Assert.True(viewModel.Workspace.SelectionSet.Contains(line.Id));

        ToolResult result = viewModel.SubmitCommandInput(string.Empty);

        Assert.NotNull(result);
        Assert.Equal("Selected entity deleted.", viewModel.LastMessage);
        Assert.False(viewModel.Workspace.Document.Entities.Contains(line.Id));
        Assert.True(viewModel.Workspace.SelectionSet.IsEmpty);
    }

    [Fact]
    public void CommandHudInput_Explode_ShouldSelectByPointerAndConfirmWithEnter()
    {
        var viewModel = new MainWindowViewModel();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5)
            },
            isClosed: false);

        viewModel.Workspace.Document.AddEntity(polyline);

        viewModel.SubmitCommandInput("EXPLODE");
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0)));

        Assert.True(viewModel.Workspace.SelectionSet.Contains(polyline.Id));

        ToolResult result = viewModel.SubmitCommandInput(string.Empty);

        Assert.NotNull(result);
        Assert.Equal("Polyline exploded into 2 entities.", viewModel.LastMessage);
        Assert.False(viewModel.Workspace.Document.Entities.Contains(polyline.Id));
        Assert.Equal(2, viewModel.Workspace.Document.Entities.All.OfType<LineEntity>().Count());
        Assert.True(viewModel.Workspace.SelectionSet.IsEmpty);
    }

    [Fact]
    public void CommandHudInput_Join_ShouldSelectByPointerAndConfirmWithEnter()
    {
        var viewModel = new MainWindowViewModel();
        var firstLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        var secondLine = new LineEntity(
            new Point2D(10, 0),
            new Point2D(20, 0));

        viewModel.Workspace.Document.AddEntity(firstLine);
        viewModel.Workspace.Document.AddEntity(secondLine);

        viewModel.SubmitCommandInput("JOIN");
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0)));
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(15, 0)));

        Assert.Equal(2, viewModel.Workspace.SelectionSet.Count);

        ToolResult result = viewModel.SubmitCommandInput(string.Empty);

        Assert.NotNull(result);
        Assert.Equal("2 entities joined into 1 polyline.", viewModel.LastMessage);
        Assert.Empty(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>());
        PolylineEntity joined = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(
            new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(20, 0) },
            joined.Vertices);
    }

    [Fact]
    public void CommandHudInput_Trim_ShouldPickBoundaryAndTarget()
    {
        var viewModel = new MainWindowViewModel();
        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        viewModel.Workspace.Document.AddEntity(boundary);
        viewModel.Workspace.Document.AddEntity(target);

        viewModel.SubmitCommandInput("TRIM");
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 2)));
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(8, 0)));

        Assert.Equal("Trim", viewModel.ActiveToolName);
        Assert.False(viewModel.Workspace.Document.Entities.Contains(target.Id));
        LineEntity trimmed = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>(),
            line => !line.Id.Equals(boundary.Id));
        Assert.True(ArePointsNear(new Point2D(0, 0), trimmed.Start));
        Assert.True(ArePointsNear(new Point2D(5, 0), trimmed.End));
    }

    [Fact]
    public void CommandHudInput_Extend_ShouldPickBoundaryAndTarget()
    {
        var viewModel = new MainWindowViewModel();
        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        viewModel.Workspace.Document.AddEntity(boundary);
        viewModel.Workspace.Document.AddEntity(target);

        viewModel.SubmitCommandInput("EXTEND");
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(10, 2)));
        viewModel.Workspace.ToolController.OnPointerPressed(
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal("Extend", viewModel.ActiveToolName);
        LineEntity extended = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.Entities.GetRequired(target.Id));
        Assert.True(ArePointsNear(new Point2D(0, 0), extended.Start));
        Assert.True(ArePointsNear(new Point2D(10, 0), extended.End));
    }

    private static void AddRectangleBoundary(MainWindowViewModel viewModel)
    {
        viewModel.Workspace.Document.AddEntity(
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        viewModel.Workspace.Document.AddEntity(
            new LineEntity(new Point2D(10, 0), new Point2D(10, 5)));
        viewModel.Workspace.Document.AddEntity(
            new LineEntity(new Point2D(10, 5), new Point2D(0, 5)));
        viewModel.Workspace.Document.AddEntity(
            new LineEntity(new Point2D(0, 5), new Point2D(0, 0)));
    }

    private static CommandHudFieldKind[] GetEditableHudFieldKinds(MainWindowViewModel viewModel)
    {
        return viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();
    }

    private static bool ArePointsNear(
        Point2D expected,
        Point2D actual,
        double tolerance = 0.000000001)
    {
        return Math.Abs(expected.X - actual.X) <= tolerance &&
               Math.Abs(expected.Y - actual.Y) <= tolerance;
    }

}
