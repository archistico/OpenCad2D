using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
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
        Assert.Equal("Specify next polyline point, press Enter to finish, or C to close.", viewModel.LastMessage);
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

        Assert.Equal("POLYLINE: Specify next point or [Close/Undo]:", viewModel.CommandPromptText);
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

    private static bool ArePointsNear(
        Point2D expected,
        Point2D actual,
        double tolerance = 0.000000001)
    {
        return Math.Abs(expected.X - actual.X) <= tolerance &&
               Math.Abs(expected.Y - actual.Y) <= tolerance;
    }

}