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
        IReadOnlyList<CommandHudFieldViewModel>? fields = null)
    {
        IsVisible = isVisible;
        ToolName = toolName;
        PromptState = promptState;
        Fields = fields ?? Array.Empty<CommandHudFieldViewModel>();
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
