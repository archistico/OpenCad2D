using System;
using System.Collections.Generic;
using System.Linq;
using OpenCad2D.Core.Anchors;

namespace OpenCad2D.App.ViewModels;

/// <summary>
/// View-model for the reusable HUD 3x3 anchor selector.
/// It can be attached to future insertion/parametric commands without duplicating
/// the anchor grid, keypad mapping or selected-state rendering.
/// </summary>
public sealed class CommandHudAnchorSelectorViewModel
{
    private static readonly IReadOnlyList<CommandHudAnchorRowViewModel> EmptyRows =
        Array.Empty<CommandHudAnchorRowViewModel>();

    private CommandHudAnchorSelectorViewModel(
        bool isVisible,
        string label,
        AnchorPoint selectedAnchor,
        IReadOnlyList<CommandHudAnchorRowViewModel> rows)
    {
        IsVisible = isVisible;
        Label = label;
        SelectedAnchor = selectedAnchor;
        Rows = rows;
    }

    public static CommandHudAnchorSelectorViewModel Hidden { get; } = new(
        isVisible: false,
        label: "Anchor",
        selectedAnchor: AnchorPoint.Center,
        rows: EmptyRows);

    public bool IsVisible { get; }

    public string Label { get; }

    public AnchorPoint SelectedAnchor { get; }

    public IReadOnlyList<CommandHudAnchorRowViewModel> Rows { get; }

    public IReadOnlyList<CommandHudAnchorOptionViewModel> Options => Rows
        .SelectMany(row => row.Options)
        .ToList();

    public static CommandHudAnchorSelectorViewModel Create(
        AnchorPoint selectedAnchor,
        string label = "Anchor")
    {
        IReadOnlyList<CommandHudAnchorRowViewModel> rows = AnchorPointService.Descriptors
            .GroupBy(descriptor => descriptor.Row)
            .OrderBy(group => group.Key)
            .Select(group => new CommandHudAnchorRowViewModel(
                group
                    .OrderBy(descriptor => descriptor.Column)
                    .Select(descriptor => new CommandHudAnchorOptionViewModel(
                        descriptor,
                        selectedAnchor))
                    .ToList()))
            .ToList();

        return new CommandHudAnchorSelectorViewModel(
            isVisible: true,
            label,
            selectedAnchor,
            rows);
    }
}
