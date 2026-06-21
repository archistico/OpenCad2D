using System;
using System.Collections.Generic;
using System.Linq;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.App.ViewModels;

/// <summary>
/// Read-only state used to drive the dynamic command HUD without coupling
/// command behaviour to Avalonia controls.
/// </summary>
public sealed class CommandHudStateViewModel
{
    public CommandHudStateViewModel(
        bool isVisible,
        string? toolName,
        CommandPromptState promptState,
        IReadOnlyList<CommandHudFieldViewModel>? fields = null,
        CommandHudAnchorSelectorViewModel? anchorSelector = null)
    {
        IsVisible = isVisible;
        ToolName = toolName;
        PromptState = promptState;
        Fields = fields ?? Array.Empty<CommandHudFieldViewModel>();
        FieldRows = BuildFieldRows(Fields);
        AnchorSelector = anchorSelector ?? CommandHudAnchorSelectorViewModel.Hidden;
        OptionViews = promptState.Options
            .Select(CommandHudOptionViewModel.FromOption)
            .ToList();
    }

    public bool IsVisible { get; }

    public string? ToolName { get; }

    public CommandPromptState PromptState { get; }

    public string Prompt => PromptState.Prompt;

    public IReadOnlyList<CommandOption> Options => PromptState.Options;

    public IReadOnlyList<CommandHudOptionViewModel> OptionViews { get; }

    public IReadOnlyList<string> OptionDisplayTexts => OptionViews
        .Select(option => option.DisplayText)
        .ToList();

    public bool HasOptions => PromptState.Options.Count > 0;

    public IReadOnlyList<CommandHudFieldViewModel> Fields { get; }

    public IReadOnlyList<CommandHudFieldRowViewModel> FieldRows { get; }

    public CommandHudAnchorSelectorViewModel AnchorSelector { get; }

    private static IReadOnlyList<CommandHudFieldRowViewModel> BuildFieldRows(
        IReadOnlyList<CommandHudFieldViewModel> fields)
    {
        if (fields.Count == 0)
        {
            return Array.Empty<CommandHudFieldRowViewModel>();
        }

        List<CommandHudFieldViewModel> geometryFields = fields
            .Where(field => field.Kind is not CommandHudFieldKind.X and not CommandHudFieldKind.Y)
            .ToList();

        List<CommandHudFieldViewModel> coordinateFields = fields
            .Where(field => field.Kind is CommandHudFieldKind.X or CommandHudFieldKind.Y)
            .OrderBy(field => field.Kind == CommandHudFieldKind.X ? 0 : 1)
            .ToList();

        List<CommandHudFieldRowViewModel> rows = new();
        AddRows(rows, geometryFields);
        AddRows(rows, coordinateFields);

        return rows;
    }

    private static void AddRows(
        List<CommandHudFieldRowViewModel> rows,
        IReadOnlyList<CommandHudFieldViewModel> fields)
    {
        for (int index = 0; index < fields.Count; index += 2)
        {
            rows.Add(new CommandHudFieldRowViewModel(fields.Skip(index).Take(2).ToList()));
        }
    }
}

public sealed class CommandHudFieldRowViewModel
{
    public CommandHudFieldRowViewModel(IReadOnlyList<CommandHudFieldViewModel> fields)
    {
        Fields = fields;
    }

    public IReadOnlyList<CommandHudFieldViewModel> Fields { get; }
}

public sealed class CommandHudOptionViewModel
{
    private CommandHudOptionViewModel(
        string shortcut,
        string remainingText,
        string displayText)
    {
        Shortcut = shortcut;
        RemainingText = remainingText;
        DisplayText = displayText;
    }

    public string Shortcut { get; }

    public string RemainingText { get; }

    public string DisplayText { get; }

    public bool HasShortcut => !string.IsNullOrWhiteSpace(Shortcut);

    public static CommandHudOptionViewModel FromOption(CommandOption option)
    {
        if (string.IsNullOrWhiteSpace(option.Shortcut))
        {
            return new CommandHudOptionViewModel(
                string.Empty,
                option.Keyword,
                option.Keyword);
        }

        string remainingText = option.Keyword.StartsWith(
            option.Shortcut,
            StringComparison.OrdinalIgnoreCase)
            ? option.Keyword[option.Shortcut.Length..]
            : option.Keyword;

        return new CommandHudOptionViewModel(
            option.Shortcut,
            remainingText,
            option.Shortcut + remainingText);
    }
}
