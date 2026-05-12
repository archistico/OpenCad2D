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

        Assert.Equal("Relative coordinates require a base point.", viewModel.LastMessage);
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

        Assert.Equal("Command input cannot be empty.", viewModel.LastMessage);
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

        Assert.Equal("Distance-angle input requires a base point.", viewModel.LastMessage);
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

    private static bool ArePointsNear(
        Point2D expected,
        Point2D actual,
        double tolerance = 0.000000001)
    {
        return Math.Abs(expected.X - actual.X) <= tolerance &&
               Math.Abs(expected.Y - actual.Y) <= tolerance;
    }

}