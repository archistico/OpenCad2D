using System.Linq;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Anchors;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.App.Tests;

public sealed class CommandHudAnchorSelectorViewModelTests
{
    [Fact]
    public void Hidden_ShouldExposeNoRows()
    {
        CommandHudAnchorSelectorViewModel selector = CommandHudAnchorSelectorViewModel.Hidden;

        Assert.False(selector.IsVisible);
        Assert.Empty(selector.Rows);
        Assert.Empty(selector.Options);
        Assert.Equal(AnchorPoint.Center, selector.SelectedAnchor);
    }

    [Fact]
    public void Create_ShouldExposeVisualGridRowsInCanonicalOrder()
    {
        CommandHudAnchorSelectorViewModel selector = CommandHudAnchorSelectorViewModel.Create(
            AnchorPoint.MiddleRight);

        Assert.True(selector.IsVisible);
        Assert.Equal("Anchor", selector.Label);
        Assert.Equal(3, selector.Rows.Count);
        Assert.All(selector.Rows, row => Assert.Equal(3, row.Options.Count));

        Assert.Equal(
            new[]
            {
                AnchorPoint.TopLeft,
                AnchorPoint.TopCenter,
                AnchorPoint.TopRight,
                AnchorPoint.MiddleLeft,
                AnchorPoint.Center,
                AnchorPoint.MiddleRight,
                AnchorPoint.BottomLeft,
                AnchorPoint.BottomCenter,
                AnchorPoint.BottomRight
            },
            selector.Options.Select(option => option.Anchor).ToArray());
    }

    [Fact]
    public void Create_ShouldMarkOnlySelectedAnchor()
    {
        CommandHudAnchorSelectorViewModel selector = CommandHudAnchorSelectorViewModel.Create(
            AnchorPoint.BottomCenter);

        CommandHudAnchorOptionViewModel selected = Assert.Single(
            selector.Options,
            option => option.IsSelected);

        Assert.Equal(AnchorPoint.BottomCenter, selected.Anchor);
        Assert.Equal("●", selected.SelectionMarker);
        Assert.Equal("2 BC", selected.DisplayText);
    }

    [Fact]
    public void State_ShouldUseHiddenAnchorSelectorByDefault()
    {
        var state = new CommandHudStateViewModel(
            isVisible: true,
            toolName: "Test",
            promptState: CommandPromptState.Idle);

        Assert.False(state.AnchorSelector.IsVisible);
        Assert.Empty(state.AnchorSelector.Rows);
    }

    [Fact]
    public void State_ShouldExposeProvidedAnchorSelector()
    {
        CommandHudAnchorSelectorViewModel selector = CommandHudAnchorSelectorViewModel.Create(
            AnchorPoint.TopLeft,
            "Insertion anchor");

        var state = new CommandHudStateViewModel(
            isVisible: true,
            toolName: "Door",
            promptState: CommandPromptState.Idle,
            anchorSelector: selector);

        Assert.True(state.AnchorSelector.IsVisible);
        Assert.Equal("Insertion anchor", state.AnchorSelector.Label);
        Assert.Equal(AnchorPoint.TopLeft, state.AnchorSelector.SelectedAnchor);
    }

    [Fact]
    public void MainWindowViewModel_ShouldAcceptAnchorShortcutWithoutChangingCurrentInsertBehavior()
    {
        var viewModel = new MainWindowViewModel();

        bool handled = viewModel.TrySelectCommandHudAnchorByShortcut(
            7,
            out var result);

        Assert.True(handled);
        Assert.Equal(AnchorPoint.TopLeft, viewModel.CommandHudSelectedAnchor);
        Assert.Equal("HUD anchor set to Top left.", viewModel.LastMessage);
        Assert.NotNull(result);
        Assert.False(viewModel.CommandHudState.AnchorSelector.IsVisible);
    }


    [Fact]
    public void MainWindowViewModel_WithDoorTool_ShouldExposeAnchorSelectorAndApplyShortcut()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetTool(ToolId.Door);

        Assert.True(viewModel.CommandHudState.AnchorSelector.IsVisible);
        Assert.Equal(AnchorPoint.MiddleLeft, viewModel.CommandHudState.AnchorSelector.SelectedAnchor);

        bool handled = viewModel.TrySelectCommandHudAnchorByShortcut(
            1,
            out var result);

        Assert.True(handled);
        Assert.Equal(AnchorPoint.BottomLeft, viewModel.CommandHudSelectedAnchor);
        Assert.Equal("Door anchor set to Bottom left.", viewModel.LastMessage);
        Assert.NotNull(result);
        Assert.True(viewModel.CommandHudState.AnchorSelector.IsVisible);
        Assert.Equal(AnchorPoint.BottomLeft, viewModel.CommandHudState.AnchorSelector.SelectedAnchor);
    }

    [Fact]
    public void MainWindowViewModel_WithWindowTool_ShouldExposeAnchorSelectorAndApplyShortcut()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SetTool(ToolId.Window);

        Assert.True(viewModel.CommandHudState.AnchorSelector.IsVisible);
        Assert.Equal(AnchorPoint.MiddleLeft, viewModel.CommandHudState.AnchorSelector.SelectedAnchor);

        bool handled = viewModel.TrySelectCommandHudAnchorByShortcut(
            3,
            out var result);

        Assert.True(handled);
        Assert.Equal(AnchorPoint.BottomRight, viewModel.CommandHudSelectedAnchor);
        Assert.Equal("Window anchor set to Bottom right.", viewModel.LastMessage);
        Assert.NotNull(result);
        Assert.True(viewModel.CommandHudState.AnchorSelector.IsVisible);
        Assert.Equal(AnchorPoint.BottomRight, viewModel.CommandHudState.AnchorSelector.SelectedAnchor);
    }

    [Fact]
    public void MainWindowViewModel_ShouldRejectInvalidAnchorShortcut()
    {
        var viewModel = new MainWindowViewModel();

        bool handled = viewModel.TrySelectCommandHudAnchorByShortcut(
            0,
            out var result);

        Assert.False(handled);
        Assert.Equal(AnchorPoint.Center, viewModel.CommandHudSelectedAnchor);
        Assert.Equal("Anchor shortcut must be one of 1, 2, 3, 4, 5, 6, 7, 8 or 9.", viewModel.LastMessage);
        Assert.NotNull(result);
    }
}
