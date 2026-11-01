using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelTransformCommandTests
{
    [Fact]
    public void SubmitCommandInput_RotateTypedPolarDestinationWithOrtho_ShouldRotateByExactAngle()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);
        viewModel.Workspace.Context.IsOrthoEnabled = true;

        viewModel.SubmitCommandInput("RO");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("@10<30");

        LineEntity rotated = Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        Assert.Equal(10 * Math.Cos(Math.PI / 6.0), rotated.End.X, precision: 9);
        Assert.Equal(5, rotated.End.Y, precision: 9);
        Assert.Contains("30", viewModel.LastMessage);
    }

    [Fact]
    public void SubmitCommandInput_EnterAfterCompletedMove_ShouldFinishMoveSoNextCommandCanStart()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);

        viewModel.SubmitCommandInput("M");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("@5,5");
        Assert.IsType<MoveTool>(viewModel.Workspace.ToolController.ActiveTool);

        viewModel.SubmitCommandInput(string.Empty);
        viewModel.SubmitCommandInput("CO");

        Assert.IsType<CopyTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(1, viewModel.Workspace.CommandHistory.UndoCount);
    }

    [Fact]
    public void SubmitCommandInput_RotateTypedAngleRightAfterBasePoint_ShouldRotate()
    {
        var viewModel = CreateViewModelWithSelectedLine(out LineEntity line);

        viewModel.SubmitCommandInput("RO");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("90");

        AssertLineEnd(viewModel, line, 0, 10);
    }

    [Fact]
    public void SubmitCommandInput_RotateReferenceOption_ShouldRotateFromReferenceToNewAngle()
    {
        var viewModel = CreateViewModelWithSelectedLine(out LineEntity line);

        viewModel.SubmitCommandInput("RO");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("R");
        viewModel.SubmitCommandInput("30");
        viewModel.SubmitCommandInput("120");

        AssertLineEnd(viewModel, line, 0, 10);
    }

    [Fact]
    public void SubmitCommandInput_RotateCopyOption_ShouldKeepOriginalAndAddRotatedCopy()
    {
        var viewModel = CreateViewModelWithSelectedLine(out LineEntity line);

        viewModel.SubmitCommandInput("RO");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("C");
        viewModel.SubmitCommandInput("90");

        Assert.Same(line, viewModel.Workspace.Document.Entities.GetRequired(line.Id));
        LineEntity copy = Assert.Single(
            viewModel.Workspace.Document.Entities.All.OfType<LineEntity>(),
            entity => !entity.Id.Equals(line.Id));
        Assert.Equal(0, copy.End.X, precision: 9);
        Assert.Equal(10, copy.End.Y, precision: 9);
    }

    [Fact]
    public void CommandHud_RotateAnglePhase_ShouldExposeAngleAsFirstEditableField()
    {
        var viewModel = CreateViewModelWithSelectedLine(out LineEntity line);

        viewModel.SubmitCommandInput("RO");
        viewModel.SubmitCommandInput("0,0");

        CommandHudFieldKind[] editableKinds = viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();

        Assert.Equal(CommandHudFieldKind.Angle, editableKinds[0]);
        Assert.Contains(CommandHudFieldKind.X, editableKinds);
        Assert.Contains(CommandHudFieldKind.Y, editableKinds);

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Angle,
            "45",
            confirm: true,
            out _));

        double expected = 10 * Math.Sqrt(0.5);
        AssertLineEnd(viewModel, line, expected, expected);
    }

    [Fact]
    public void SubmitCommandInput_ScaleTypedFactorRightAfterBasePoint_ShouldScale()
    {
        var viewModel = CreateViewModelWithSelectedLine(out LineEntity line);

        viewModel.SubmitCommandInput("SC");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("2");

        AssertLineEnd(viewModel, line, 20, 0);
    }

    [Fact]
    public void SubmitCommandInput_ScaleReferenceOption_ShouldScaleByNewOverReferenceLength()
    {
        var viewModel = CreateViewModelWithSelectedLine(out LineEntity line);

        viewModel.SubmitCommandInput("SC");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("R");
        viewModel.SubmitCommandInput("4");
        viewModel.SubmitCommandInput("2");

        AssertLineEnd(viewModel, line, 5, 0);
    }

    [Fact]
    public void CommandHud_ScaleReferenceLengthPhase_ShouldAcceptDistanceField()
    {
        var viewModel = CreateViewModelWithSelectedLine(out LineEntity line);

        viewModel.SubmitCommandInput("SC");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SubmitCommandInput("R");

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "10",
            confirm: true,
            out _));

        ScaleTool tool = Assert.IsType<ScaleTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(ScaleToolState.WaitingForNewLength, tool.State);

        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.Distance,
            "30",
            confirm: true,
            out _));

        AssertLineEnd(viewModel, line, 30, 0);
    }

    private static MainWindowViewModel CreateViewModelWithSelectedLine(out LineEntity line)
    {
        var viewModel = new MainWindowViewModel();
        line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);
        return viewModel;
    }

    private static void AssertLineEnd(MainWindowViewModel viewModel, LineEntity original, double x, double y)
    {
        LineEntity current = Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.GetRequired(original.Id));
        Assert.Equal(0, current.Start.X, precision: 9);
        Assert.Equal(0, current.Start.Y, precision: 9);
        Assert.Equal(x, current.End.X, precision: 9);
        Assert.Equal(y, current.End.Y, precision: 9);
    }
}
