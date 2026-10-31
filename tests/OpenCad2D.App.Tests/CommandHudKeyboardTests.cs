using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenCad2D.App.Controls;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Architectural;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.App.Tests;

public sealed class CommandHudTestSession : IDisposable
{
    public HeadlessUnitTestSession Session { get; } = HeadlessUnitTestSession.StartNew(typeof(CommandHudTestApplication));

    public void Dispose() => Session.Dispose();
}

public static class CommandHudTestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class CommandHudKeyboardTests : IClassFixture<CommandHudTestSession>
{
    private readonly CommandHudTestSession _fixture;

    public CommandHudKeyboardTests(CommandHudTestSession fixture) => _fixture = fixture;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Extend_HoverPreview_ShouldDrawSolidExistingPartDashedAdditionAndRedBoundary(bool all) => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareExtendDrawing(window, out LineEntity target);
        StartCommand(window, "EXTEND");
        if (all)
            Press(window, Key.Enter);
        else
            TrimPointer(window, new Point(350, 120), MouseButton.Left);
        TrimPointer(window, new Point(240, 200));
        var drawing = new DrawingGroup();
        using (DrawingContext context = drawing.Open())
            window.FindControl<CadCanvas>("CadCanvas")!.Render(context);
        GeometryDrawing[] strokes = GetDrawingLeaves(drawing).OfType<GeometryDrawing>().ToArray();
        Assert.Contains(strokes, stroke => IsLine(stroke, new Point(100, 200), new Point(250, 200)) &&
            HasStrokeColor(stroke, Color.FromRgb(100, 220, 120)) && stroke.Pen?.DashStyle is null);
        Assert.Contains(strokes, stroke => IsLine(stroke, new Point(250, 200), new Point(350, 200)) &&
            HasStrokeColor(stroke, Color.FromRgb(100, 220, 120)) && stroke.Pen?.DashStyle?.Dashes?.Count > 0);
        Assert.Contains(strokes, stroke => IsLine(stroke, new Point(350, 100), new Point(350, 300)) &&
            HasStrokeColor(stroke, Color.FromRgb(255, 90, 90)));
        Assert.DoesNotContain(strokes, stroke => IsLine(stroke, new Point(100, 200), new Point(350, 200)));
        Assert.Same(target, viewModel.Workspace.Document.Entities.GetRequired(target.Id));
        Assert.False(viewModel.IsDirty);
        TrimPointer(window, new Point(450, 250));
        Assert.Empty(Assert.IsType<ExtendTool>(viewModel.Workspace.ToolController.ActiveTool)
            .GetPreviewDescriptor(viewModel.Workspace.Context).ReplacedEntityIds);
    });

    [Fact]
    public Task Extend_AllExtendEdgesUndoAndFinish_ShouldPreserveDirtyStateAndKeyboardWorkflow() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareExtendDrawing(window, out LineEntity target);
        StartCommand(window, "EXTEND");
        Assert.Contains("1/2", viewModel.CommandHudState.Prompt);
        Press(window, Key.A);
        Assert.Contains("2/2", viewModel.CommandHudState.Prompt);
        TrimPointer(window, new Point(240, 200), MouseButton.Left);
        Assert.Equal(new Point2D(350, 200), Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.GetRequired(target.Id)).End);
        Assert.True(viewModel.IsDirty);
        Assert.IsType<ExtendTool>(viewModel.Workspace.ToolController.ActiveTool);
        Press(window, Key.E);
        Assert.Contains("1/2", viewModel.CommandHudState.Prompt);
        Press(window, Key.U);
        Assert.Same(target, viewModel.Workspace.Document.Entities.GetRequired(target.Id));
        Assert.False(viewModel.IsDirty);
        TrimPointer(window, new Point(350, 120), MouseButton.Left);
        Press(window, Key.Enter);
        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
    });

    [Fact]
    public Task Extend_RightClick_ShouldStartAllThenFinishToSelection() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareExtendDrawing(window, out _);
        StartCommand(window, "EXTEND");
        TrimPointer(window, new Point(200, 150), MouseButton.Right);
        Assert.Equal(ExtendToolState.WaitingForTargetEntity, Assert.IsType<ExtendTool>(viewModel.Workspace.ToolController.ActiveTool).State);
        TrimPointer(window, new Point(200, 150), MouseButton.Right);
        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.False(viewModel.IsDirty);
    });

    [Fact]
    public Task Extend_MiddleHover_ShouldShowActionableHudFeedbackThenRecover() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareExtendDrawing(window, out _);
        StartCommand(window, "EXTEND");
        TrimPointer(window, new Point(350, 120), MouseButton.Left);
        TrimPointer(window, new Point(175, 200));
        TextBlock feedback = window.FindControl<TextBlock>("HudCommandFeedback")!;
        Assert.True(feedback.IsVisible);
        Assert.Contains("ambiguous", feedback.Text);
        Assert.False(Assert.IsType<ExtendTool>(viewModel.Workspace.ToolController.ActiveTool).HasPreview);
        TrimPointer(window, new Point(240, 200));
        Assert.Contains("add the dashed green", feedback.Text);
    });

    private static MainWindowViewModel PrepareExtendDrawing(MainWindow window, out LineEntity target)
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        window.FindControl<CadCanvas>("CadCanvas")!.ApplyViewportState(
            new OpenCad2D.Persistence.Dto.ViewportStateDto { Zoom = 1, PanX = 0, PanY = 0 });
        target = new LineEntity(new Point2D(100, 200), new Point2D(250, 200));
        viewModel.Workspace.Document.AddEntity(new LineEntity(new Point2D(350, 100), new Point2D(350, 300)));
        viewModel.Workspace.Document.AddEntity(target);
        viewModel.Workspace.MarkSaved();
        return viewModel;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Trim_HoverPreview_ShouldDrawGreenResultAndDashedRemovalWithRedEdges(bool all) => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareTrimDrawing(window, out LineEntity target);
        StartCommand(window, "TRIM");
        if (all)
            Press(window, Key.A);
        else
            TrimPointer(window, new Point(250, 120), MouseButton.Left);
        TrimPointer(window, new Point(350, 200));
        CadCanvas canvas = window.FindControl<CadCanvas>("CadCanvas")!;
        var drawing = new DrawingGroup();
        using (DrawingContext context = drawing.Open())
        {
            canvas.Render(context);
        }

        GeometryDrawing[] strokes = GetDrawingLeaves(drawing).OfType<GeometryDrawing>().ToArray();
        Assert.Contains(strokes, stroke => IsLine(stroke, new Point(100, 200), new Point(250, 200)) &&
            HasStrokeColor(stroke, Color.FromRgb(100, 220, 120)) && stroke.Pen?.DashStyle is null);
        Assert.Contains(strokes, stroke => IsLine(stroke, new Point(250, 200), new Point(400, 200)) &&
            HasStrokeColor(stroke, Color.FromRgb(100, 220, 120)) && stroke.Pen?.DashStyle?.Dashes?.Count > 0);
        Assert.Contains(strokes, stroke => IsLine(stroke, new Point(250, 100), new Point(250, 300)) &&
            HasStrokeColor(stroke, Color.FromRgb(255, 90, 90)) && stroke.Pen?.DashStyle is null);
        Assert.DoesNotContain(strokes, stroke => IsLine(stroke, new Point(100, 200), new Point(400, 200)));
        Assert.Same(target, viewModel.Workspace.Document.Entities.GetRequired(target.Id));
        Assert.False(viewModel.IsDirty);

        TrimPointer(window, new Point(450, 250));
        ToolPreviewDescriptor cleared = Assert.IsType<TrimTool>(viewModel.Workspace.ToolController.ActiveTool)
            .GetPreviewDescriptor(viewModel.Workspace.Context);
        Assert.Empty(cleared.ReplacedEntityIds);
        Assert.Empty(cleared.Entities);
        Assert.Empty(cleared.HighlightedEntities);
    });

    private static IEnumerable<Drawing> GetDrawingLeaves(Drawing drawing)
    {
        if (drawing is DrawingGroup group)
        {
            return group.Children.SelectMany(GetDrawingLeaves);
        }
        return new[] { drawing };
    }

    private static bool IsLine(GeometryDrawing drawing, Point start, Point end)
    {
        // DrawingGroup records DrawLine as platform geometry rather than LineGeometry.
        return drawing.Geometry?.Bounds == new Rect(start, end);
    }

    private static bool HasStrokeColor(GeometryDrawing drawing, Color color)
    {
        return drawing.Pen?.Brush is ISolidColorBrush brush && brush.Color == color;
    }

    [Fact]
    public Task Fillet_ExcessiveRadius_ShouldShowFailureAndAllowRadiusRecovery() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareFilletDrawing(window, out LineEntity first, out _);
        StartCommand(window, "FILLET");
        Press(window, Key.R);
        window.KeyTextInput("200");
        Press(window, Key.Enter);
        TrimPointer(window, new Point(150, 200), MouseButton.Left);
        TrimPointer(window, new Point(250, 250), MouseButton.Left);

        FilletTool tool = Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Contains("too large", window.FindControl<TextBlock>("HudCommandFeedback")!.Text);
        Assert.False(viewModel.IsDirty);
        Press(window, Key.R);
        window.KeyTextInput("20");
        Press(window, Key.Enter);
        Assert.Equal(first.Id, tool.FirstEntityId);
        Assert.Equal(FilletToolState.WaitingForSecondEntity, tool.State);
        Assert.Contains("2/2", viewModel.CommandHudState.Prompt);
        TrimPointer(window, new Point(250, 250), MouseButton.Left);

        Assert.Equal(20, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<ArcEntity>()).Radius);
        Assert.True(viewModel.IsDirty);
        viewModel.Undo();
        Assert.False(viewModel.IsDirty);
        Assert.Equal(2, viewModel.Workspace.Document.Entities.Count);
        viewModel.Redo();
        Assert.Equal(3, viewModel.Workspace.Document.Entities.Count);
        Press(window, Key.Enter);
        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
    });

    [Fact]
    public Task Fillet_NoTrimAfterFirstPick_ShouldKeepOriginalsAndFinishOnRightClick() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareFilletDrawing(window, out LineEntity first, out LineEntity second);
        StartFilletWithRadius(window, "20");
        TrimPointer(window, new Point(150, 200), MouseButton.Left);
        Press(window, Key.T);
        Press(window, Key.N);
        Assert.Equal(first.Id, Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool).FirstEntityId);
        TrimPointer(window, new Point(250, 250), MouseButton.Left);

        Assert.Same(first, viewModel.Workspace.Document.Entities.GetRequired(first.Id));
        Assert.Same(second, viewModel.Workspace.Document.Entities.GetRequired(second.Id));
        Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<ArcEntity>());
        TrimPointer(window, new Point(300, 300), MouseButton.Right);
        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
    });

    [Fact]
    public Task Fillet_FirstOption_ShouldReplaceWrongFirstSelectionAndKeepSettings() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareFilletDrawing(window, out _, out LineEntity second);
        StartFilletWithRadius(window, "20");
        TrimPointer(window, new Point(150, 200), MouseButton.Left);
        Press(window, Key.F);
        FilletTool tool = Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Null(tool.FirstEntityId);
        Assert.Equal(20, tool.Radius);
        Assert.Contains("1/2", viewModel.CommandHudState.Prompt);
        TrimPointer(window, new Point(250, 250), MouseButton.Left);
        Assert.Equal(second.Id, tool.FirstEntityId);
        TrimPointer(window, new Point(150, 200), MouseButton.Left);
        Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<ArcEntity>());
    });

    [Fact]
    public Task Fillet_UntouchedRadiusConfirm_ShouldResumeSecondSelection() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareFilletDrawing(window, out LineEntity first, out _);
        StartFilletWithRadius(window, "20");
        TrimPointer(window, new Point(150, 200), MouseButton.Left);
        Press(window, Key.R);
        Press(window, Key.Tab);
        Press(window, Key.Enter);

        FilletTool tool = Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(20, tool.Radius);
        Assert.Equal(first.Id, tool.FirstEntityId);
        Assert.Equal(FilletToolState.WaitingForSecondEntity, tool.State);
        Assert.False(viewModel.IsDirty);
        Press(window, Key.Enter);
        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
    });

    [Fact]
    public Task Fillet_SmallRadius_ShouldRemainVisibleAndPreservePrecision() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareFilletDrawing(window, out _, out _);
        StartFilletWithRadius(window, "0.00001");
        Press(window, Key.R);
        Press(window, Key.Tab);
        TextBox radius = AssertFocusedField(window, CommandHudFieldKind.Radius);
        Assert.Equal(0.00001, double.Parse(radius.Text!, System.Globalization.CultureInfo.CurrentCulture));
        Press(window, Key.Enter);
        Assert.Equal(0.00001, Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool).Radius);
        Assert.False(viewModel.IsDirty);
    });

    [Fact]
    public Task Fillet_InvalidRadiusAfterFirstPick_ShouldKeepDraftAndSelectedObject() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareFilletDrawing(window, out LineEntity first, out _);
        StartFilletWithRadius(window, "20");
        TrimPointer(window, new Point(150, 200), MouseButton.Left);
        Press(window, Key.R);
        Press(window, Key.Tab);
        window.KeyTextInput("-5");
        Press(window, Key.Enter);

        Assert.True(viewModel.HasCommandHudValidationMessage);
        TextBox field = AssertFocusedField(window, CommandHudFieldKind.Radius);
        Assert.Equal("-5", field.Text);
        Assert.Equal(first.Id, Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool).FirstEntityId);
        field.SelectAll();
        window.KeyTextInput("10");
        Press(window, Key.Enter);
        Assert.Equal(FilletToolState.WaitingForSecondEntity, Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool).State);
        Assert.False(viewModel.IsDirty);
    });

    private static MainWindowViewModel PrepareFilletDrawing(MainWindow window, out LineEntity first, out LineEntity second)
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        window.FindControl<CadCanvas>("CadCanvas")!.ApplyViewportState(
            new OpenCad2D.Persistence.Dto.ViewportStateDto { Zoom = 1, PanX = 0, PanY = 0 });
        first = new LineEntity(new Point2D(100, 200), new Point2D(250, 200));
        second = new LineEntity(new Point2D(250, 200), new Point2D(250, 350));
        viewModel.Workspace.Document.AddEntity(first);
        viewModel.Workspace.Document.AddEntity(second);
        viewModel.Workspace.MarkSaved();
        return viewModel;
    }

    private static void StartFilletWithRadius(MainWindow window, string radius)
    {
        StartCommand(window, "FILLET");
        Press(window, Key.R);
        window.KeyTextInput(radius);
        Press(window, Key.Enter);
    }

    [Fact]
    public Task Trim_EnterForAllCutUndoAndFinish_ShouldPreserveWorkflowAndDirtyState() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareTrimDrawing(window, out LineEntity target);
        StartCommand(window, "TRIM");
        Assert.Contains("1/2", viewModel.CommandHudState.Prompt);
        Press(window, Key.Enter);
        Assert.Contains("2/2", viewModel.CommandHudState.Prompt);
        TrimPointer(window, new Point(350, 200), MouseButton.Left);
        Assert.False(viewModel.Workspace.Document.Entities.Contains(target.Id));
        Assert.True(viewModel.IsDirty);

        Press(window, Key.U);
        Assert.True(viewModel.Workspace.Document.Entities.Contains(target.Id));
        Assert.False(viewModel.IsDirty);
        Press(window, Key.Enter);
        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
    });

    [Fact]
    public Task Trim_EdgesOption_ShouldRecoverWrongBoundaryWithoutLosingUndo() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareTrimDrawing(window, out LineEntity target);
        StartCommand(window, "TRIM");
        TrimPointer(window, new Point(250, 120), MouseButton.Left);
        TrimPointer(window, new Point(350, 200), MouseButton.Left);
        Assert.False(viewModel.Workspace.Document.Entities.Contains(target.Id));

        Press(window, Key.E);
        Assert.Contains("1/2", viewModel.CommandHudState.Prompt);
        Assert.Contains(viewModel.CommandHudState.Options, option => option.Keyword == "Undo");
        Press(window, Key.U);
        Assert.True(viewModel.Workspace.Document.Entities.Contains(target.Id));
        Assert.False(viewModel.IsDirty);
    });

    [Fact]
    public Task Trim_RightClick_ShouldStartAllThenFinishToSelection() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareTrimDrawing(window, out _);
        StartCommand(window, "TRIM");
        TrimPointer(window, new Point(200, 150), MouseButton.Right);
        Assert.Equal(TrimToolState.WaitingForTargetEntity, Assert.IsType<TrimTool>(viewModel.Workspace.ToolController.ActiveTool).State);
        TrimPointer(window, new Point(200, 150), MouseButton.Right);
        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.False(viewModel.IsDirty);
    });

    [Fact]
    public Task Trim_InvalidHover_ShouldShowActionableFeedbackInsideHud() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareTrimDrawing(window, out _);
        viewModel.Workspace.Document.AddEntity(new LineEntity(new Point2D(100, 350), new Point2D(400, 350)));
        StartCommand(window, "TRIM");
        TrimPointer(window, new Point(250, 120), MouseButton.Left);
        TrimPointer(window, new Point(350, 350));

        TextBlock feedback = window.FindControl<TextBlock>("HudCommandFeedback")!;
        Assert.True(feedback.IsVisible);
        Assert.Contains("No trim intersection", feedback.Text);
        Assert.False(Assert.IsType<TrimTool>(viewModel.Workspace.ToolController.ActiveTool).HasPreview);
        TrimPointer(window, new Point(350, 200));
        Assert.Contains("Click the dashed", feedback.Text);
    });

    private static MainWindowViewModel PrepareTrimDrawing(MainWindow window, out LineEntity target)
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        window.FindControl<CadCanvas>("CadCanvas")!.ApplyViewportState(
            new OpenCad2D.Persistence.Dto.ViewportStateDto { Zoom = 1, PanX = 0, PanY = 0 });
        target = new LineEntity(new Point2D(100, 200), new Point2D(400, 200));
        viewModel.Workspace.Document.AddEntity(new LineEntity(new Point2D(250, 100), new Point2D(250, 300)));
        viewModel.Workspace.Document.AddEntity(target);
        viewModel.Workspace.MarkSaved();
        return viewModel;
    }

    private static void TrimPointer(MainWindow window, Point local, MouseButton? button = null)
    {
        CadCanvas canvas = window.FindControl<CadCanvas>("CadCanvas")!;
        Point position = canvas.TranslatePoint(local, window)!.Value;
        window.MouseMove(position);
        if (button is not null)
        {
            window.MouseDown(position, button.Value);
            window.MouseUp(position, button.Value);
        }
        Refresh(window);
    }

    [Fact]
    public Task Line_DistanceTabAngleEnter_ShouldCreateExactSegment() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("25");
        Press(window, Key.Tab);
        AssertFocusedField(window, CommandHudFieldKind.Angle);
        window.KeyTextInput("90");
        Press(window, Key.Enter);

        LineEntity line = Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>());
        Assert.Equal(0, line.End.X, 8);
        Assert.Equal(25, line.End.Y, 8);
    });

    [Fact]
    public Task Line_ShiftTab_ShouldMoveBackAndKeepDistance() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("25");
        Press(window, Key.Tab);
        window.KeyTextInput("90");
        Press(window, Key.Tab, RawInputModifiers.Shift);
        AssertFocusedField(window, CommandHudFieldKind.Distance);
        Press(window, Key.Enter);

        LineEntity line = Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>());
        Assert.Equal(25, line.End.Y, 8);
    });

    [Fact]
    public Task Line_InvalidDistance_ShouldKeepDraftAndAllowCorrection() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("20");
        Press(window, Key.Tab);
        Press(window, Key.Tab, RawInputModifiers.Shift);
        window.KeyTextInput("-5");
        Press(window, Key.Enter);

        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        TextBox field = AssertFocusedField(window, CommandHudFieldKind.Distance);
        Assert.Equal("-5", field.Text);
        TextBlock validation = window.FindControl<TextBlock>("HudValidationMessage")!;
        Assert.True(validation.IsVisible);
        Assert.Contains("greater than zero", validation.Text);
        Press(window, Key.Tab);
        AssertFocusedField(window, CommandHudFieldKind.Distance);
        field.SelectAll();
        window.KeyTextInput("30");
        Press(window, Key.Enter);
        Assert.Equal(30, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()).End.X, 8);
        Assert.False(validation.IsVisible);
    });

    [Fact]
    public Task Polygon_TabThenEnter_ShouldKeepTypedSideCount() => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, "POLYGON");
        Refresh(window);
        window.KeyTextInput("12");
        Press(window, Key.Tab);
        Press(window, Key.Enter);

        PolygonTool tool = Assert.IsType<PolygonTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Equal(12, tool.SideCount);
        Assert.Equal(PolygonToolState.WaitingForCenter, tool.State);
    });

    [Fact]
    public Task Divide_TabThenPointerMoveAndEnter_ShouldKeepSegmentCount() => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        var line = new LineEntity(new Point2D(0, 0), new Point2D(120, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.Select(line.Id);
        StartCommand(window, "DIVIDE");
        window.KeyTextInput("12");
        Press(window, Key.Tab);
        MovePointer(window);
        Press(window, Key.Enter);

        Assert.Equal(11, viewModel.Workspace.Document.Entities.All.OfType<PointEntity>().Count());
        Assert.Same(line, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()));
    });

    [Fact]
    public Task Polyline_OptionFromFocusedField_ShouldChangeModeAndClearOverrides() => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, "PL");
        viewModel.SubmitCommandInput("0,0");
        Refresh(window);
        window.KeyTextInput("20");
        Press(window, Key.Tab);
        Press(window, Key.A);

        Assert.Contains("arc", viewModel.CommandPromptText, StringComparison.OrdinalIgnoreCase);
        Assert.False(viewModel.HasCommandHudInputOverrides);
    });

    [Fact]
    public Task Line_TypedPolarExpression_ShouldUseCommandParser() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("10");
        window.KeyTextInput("<");
        window.KeyTextInput("90");
        Press(window, Key.Enter);

        LineEntity line = Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>());
        Assert.Equal(10, line.End.Y, 8);
    });

    [Theory]
    [InlineData("10", ",", "5", 10, 5)]
    [InlineData("@10,5", "", "", 10, 5)]
    [InlineData("10,5", "", "", 10, 5)]
    public Task Line_TypedCoordinates_ShouldPreserveCompleteExpression(
        string first, string separator, string last, double x, double y) => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput(first);
        if (separator.Length > 0) window.KeyTextInput(separator);
        if (last.Length > 0) window.KeyTextInput(last);
        Press(window, Key.Enter);

        Assert.Equal(new Point2D(x, y), Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()).End);
    });

    [Fact]
    public Task Line_DecimalCommaInFocusedField_ShouldAcceptScalar() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        Press(window, Key.Tab);
        window.KeyTextInput("12,5");
        Press(window, Key.Enter);
        Assert.Equal(12.5, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()).End.X, 8);
    });

    [Fact]
    public Task Polygon_InvalidSides_ShouldBlockTabAndKeepDraft() => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, "POLYGON");
        window.KeyTextInput("257");
        Press(window, Key.Tab);
        Assert.Equal(PolygonToolState.WaitingForSides, Assert.IsType<PolygonTool>(viewModel.Workspace.ToolController.ActiveTool).State);
        Press(window, Key.Back);
        Press(window, Key.Enter);
        Assert.Equal(25, Assert.IsType<PolygonTool>(viewModel.Workspace.ToolController.ActiveTool).SideCount);
    });

    [Fact]
    public Task Line_EmptyEditedField_ShouldRejectEnterWithoutUsingOldDistance() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("25");
        Press(window, Key.Tab);
        Press(window, Key.Tab, RawInputModifiers.Shift);
        Press(window, Key.Back);
        Press(window, Key.Enter);

        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.Equal(string.Empty, AssertFocusedField(window, CommandHudFieldKind.Distance).Text);
    });

    [Fact]
    public Task Line_PointerMoveWhileEditing_ShouldKeepFocusDraftAndCaret() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        Press(window, Key.Tab);
        window.KeyTextInput("12.3456789");
        TextBox field = AssertFocusedField(window, CommandHudFieldKind.Distance);
        field.CaretIndex = 3;
        MovePointer(window);

        Assert.Same(field, AssertFocusedField(window, CommandHudFieldKind.Distance));
        Assert.Equal("12.3456789", field.Text);
        Assert.Equal(3, field.CaretIndex);
        Press(window, Key.Tab);
        Press(window, Key.Enter);
        Assert.Equal(12.3456789, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()).End.X, 8);
    });

    [Fact]
    public Task Line_RightClick_ShouldConfirmTypedDistance() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("25");
        RightClick(window);

        Assert.Equal(25, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()).End.X, 8);
    });

    [Fact]
    public Task Line_LeftClickWithPendingDistance_ShouldUseExactValue() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("25");
        ClickCanvas(window, MouseButton.Left);

        Assert.Equal(25, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()).End.X, 8);
    });

    [Fact]
    public Task Line_LeftClickWithInvalidDraft_ShouldRejectPointAndKeepDraft() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        Press(window, Key.Tab);
        window.KeyTextInput("-5");
        ClickCanvas(window, MouseButton.Left);

        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.Equal("-5", AssertFocusedField(window, CommandHudFieldKind.Distance).Text);
        Assert.True(viewModel.HasCommandHudValidationMessage);
    });

    [Fact]
    public Task Line_RightClickWithInvalidDraft_ShouldKeepCommandAndDraft() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        Press(window, Key.Tab);
        window.KeyTextInput("-5");
        RightClick(window);

        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.Equal("-5", AssertFocusedField(window, CommandHudFieldKind.Distance).Text);
    });

    [Fact]
    public Task Point_TabThroughUntouchedFields_ShouldRequireBothCoordinates() => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, "POINT");
        Refresh(window);
        Press(window, Key.Tab);
        window.KeyTextInput("12");
        Press(window, Key.Tab);
        Press(window, Key.Enter);

        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.Contains("both X and Y", viewModel.LastMessage);
        window.KeyTextInput("-3.5");
        Press(window, Key.Enter);
        Assert.Equal(new Point2D(12, -3.5), Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<PointEntity>()).Position);
    });

    [Fact]
    public Task Line_BackspaceToEmptyField_ShouldKeepOtherOverride() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("25");
        Press(window, Key.Tab);
        window.KeyTextInput("90");
        Press(window, Key.Back);
        Press(window, Key.Back);
        Press(window, Key.Tab);
        Press(window, Key.Enter);

        Assert.Equal(25, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<LineEntity>()).End.X, 8);
    });

    [Theory]
    [InlineData("RECTANGLE", "20", "10")]
    [InlineData("CIRCLE", "20", null)]
    public Task Drawing_TypedDimensions_ShouldCreateUndoableGeometry(string command, string first, string? second) => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, command);
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(5, 5));
        Refresh(window);
        window.KeyTextInput(first);
        if (second is not null)
        {
            Press(window, Key.Tab);
            AssertFocusedField(window, CommandHudFieldKind.Height);
            Assert.Equal(20, Assert.Single(viewModel.CommandHudState.Fields, field => field.Kind == CommandHudFieldKind.Width).LiveValue);
            window.KeyTextInput(second);
            Refresh(window);
            Assert.Equal("10", AssertFocusedField(window, CommandHudFieldKind.Height).Text);
        }
        Press(window, Key.Enter);

        Assert.True(viewModel.Workspace.Document.Entities.All.Any(), viewModel.LastMessage);
        Assert.Single(viewModel.Workspace.Document.Entities.All);
        if (command == "CIRCLE")
        {
            Assert.Equal(20, Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<CircleEntity>()).Radius, 8);
        }
        else
        {
            PolylineEntity rectangle = Assert.Single(viewModel.Workspace.Document.Entities.All.OfType<PolylineEntity>());
            Assert.Equal(new Point2D(20, 10), rectangle.Vertices[2]);
        }
        Assert.True(viewModel.IsDirty);
        viewModel.Undo();
        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.False(viewModel.IsDirty);
        viewModel.Redo();
        Assert.Single(viewModel.Workspace.Document.Entities.All);
    });

    [Theory]
    [InlineData("DOOR", Key.W, "85.5")]
    [InlineData("WINDOW", Key.T, "22.5")]
    public Task NumericOption_TabAndPointerMove_ShouldKeepValueUntilEnter(string command, Key option, string value) => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, command);
        Refresh(window);
        Press(window, Key.Tab);
        Press(window, option);
        window.KeyTextInput(value);
        Press(window, Key.Tab);
        MovePointer(window);
        Press(window, Key.Enter);

        if (command == "DOOR")
        {
            DoorTool door = Assert.IsType<DoorTool>(viewModel.Workspace.ToolController.ActiveTool);
            Assert.Equal(85.5, door.CurrentWidth);
            Assert.Equal(DoorToolState.WaitingForInsertionPoint, door.State);
        }
        else
        {
            WindowTool tool = Assert.IsType<WindowTool>(viewModel.Workspace.ToolController.ActiveTool);
            Assert.Equal(22.5, tool.CurrentWallThickness);
            Assert.Equal(WindowToolState.WaitingForInsertionPoint, tool.State);
        }
        Assert.False(viewModel.IsDirty);
    });

    [Fact]
    public Task Door_InvalidDomainValue_ShouldStayEditableUntilCorrected() => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, "DOOR");
        Refresh(window);
        Press(window, Key.O);
        Press(window, Key.Tab);
        window.KeyTextInput("361");
        Press(window, Key.Enter);
        TextBox field = AssertFocusedField(window, CommandHudFieldKind.Number);
        Assert.Equal("361", field.Text);
        Assert.Equal(DoorToolState.WaitingForOpeningAngle, Assert.IsType<DoorTool>(viewModel.Workspace.ToolController.ActiveTool).State);
        field.SelectAll();
        window.KeyTextInput("60");
        Press(window, Key.Enter);
        Assert.Equal(60, Assert.IsType<DoorTool>(viewModel.Workspace.ToolController.ActiveTool).CurrentOpeningAngleDegrees);
    });

    [Fact]
    public Task Fillet_TabThenUntouchedEnter_ShouldUseDefaultRadius() => Run(window =>
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, "FILLET");
        Refresh(window);
        Press(window, Key.R);
        Press(window, Key.Tab);
        Press(window, Key.Enter);

        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, Assert.IsType<FilletTool>(viewModel.Workspace.ToolController.ActiveTool).State);
        Assert.False(viewModel.HasCommandHudInputOverrides);
    });

    [Fact]
    public Task Line_EscapeWhileEditing_ShouldCancelWithoutGeometry() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        window.KeyTextInput("25");
        Press(window, Key.Tab);
        window.KeyTextInput("90");
        Press(window, Key.Escape);

        Assert.IsType<SelectionTool>(viewModel.Workspace.ToolController.ActiveTool);
        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.False(viewModel.HasCommandHudInputOverrides);
    });

    [Fact]
    public Task TextEditor_DuringCommand_ShouldKeepKeyboardInputLocal() => Run(window =>
    {
        MainWindowViewModel viewModel = PrepareLine(window);
        var editor = new TextBox { Text = "Label" };
        Panel host = Assert.IsAssignableFrom<Panel>(window.Content);
        host.Children.Add(editor);
        try
        {
            editor.Focus();
            window.KeyTextInput("25");
            Press(window, Key.Enter);
            Press(window, Key.Up);
            Assert.Empty(viewModel.Workspace.Document.Entities.All);
            Assert.False(viewModel.HasCommandHudInputOverrides);
            Assert.Contains("25", editor.Text);
        }
        finally
        {
            host.Children.Remove(editor);
        }
    });

    private static void MovePointer(MainWindow window)
    {
        CadCanvas canvas = window.FindControl<CadCanvas>("CadCanvas")!;
        window.MouseMove(canvas.TranslatePoint(new Point(250, 200), window)!.Value);
        Refresh(window);
    }

    private static void RightClick(MainWindow window)
    {
        ClickCanvas(window, MouseButton.Right);
    }

    private static void ClickCanvas(MainWindow window, MouseButton button)
    {
        CadCanvas canvas = window.FindControl<CadCanvas>("CadCanvas")!;
        Point position = canvas.TranslatePoint(new Point(250, 200), window)!.Value;
        window.MouseDown(position, button);
        window.MouseUp(position, button);
        Refresh(window);
    }

    private Task Run(Action<MainWindow> test) => _fixture.Session.Dispatch(() =>
    {
        var window = new MainWindow();
        window.Show();
        try
        {
            Refresh(window);
            GetViewModel(window).Workspace.MarkSaved();
            CadCanvas canvas = window.FindControl<CadCanvas>("CadCanvas")!;
            window.MouseMove(canvas.TranslatePoint(new Point(150, 150), window)!.Value);
            canvas.Focus();
            test(window);
        }
        finally
        {
            // Prevent the save dialog when closing the test window.
            GetViewModel(window).Workspace.MarkSaved();
            window.Close();
        }
    }, CancellationToken.None);

    private static void StartCommand(MainWindow window, string command)
    {
        window.KeyTextInput(command);
        Press(window, Key.Enter);
    }

    private static MainWindowViewModel PrepareLine(MainWindow window)
    {
        MainWindowViewModel viewModel = GetViewModel(window);
        StartCommand(window, "LINE");
        viewModel.SubmitCommandInput("0,0");
        viewModel.SetMousePosition(new Point2D(10, 0));
        Refresh(window);
        return viewModel;
    }

    private static MainWindowViewModel GetViewModel(MainWindow window) => Assert.IsType<MainWindowViewModel>(window.DataContext);

    private static void Press(MainWindow window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Refresh(window);
    }

    private static void Refresh(MainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static TextBox AssertFocusedField(MainWindow window, CommandHudFieldKind kind)
    {
        Assert.True(window.GetVisualDescendants().OfType<TextBox>().Any(textBox => textBox.IsFocused),
            string.Join("; ", window.GetVisualDescendants().OfType<TextBox>()
                .Where(textBox => textBox.DataContext is CommandHudFieldViewModel)
                .Select(textBox => $"{((CommandHudFieldViewModel)textBox.DataContext!).Kind}: visible={textBox.IsEffectivelyVisible} focusable={textBox.Focusable} enabled={textBox.IsEffectivelyEnabled} text={textBox.Text}")) +
            $" panel={window.FindControl<Border>("CommandHudPanel")!.IsVisible} focus={window.FocusManager!.GetFocusedElement()} message={GetViewModel(window).LastMessage}");
        TextBox field = Assert.Single(window.GetVisualDescendants().OfType<TextBox>(), textBox => textBox.IsFocused);
        Assert.Equal(kind, Assert.IsType<CommandHudFieldViewModel>(field.DataContext).Kind);
        return field;
    }
}
