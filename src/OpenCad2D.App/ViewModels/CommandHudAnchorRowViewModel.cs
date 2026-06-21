using System.Collections.Generic;

namespace OpenCad2D.App.ViewModels;

/// <summary>
/// One row of the command HUD 3x3 anchor selector.
/// </summary>
public sealed class CommandHudAnchorRowViewModel
{
    public CommandHudAnchorRowViewModel(IReadOnlyList<CommandHudAnchorOptionViewModel> options)
    {
        Options = options;
    }

    public IReadOnlyList<CommandHudAnchorOptionViewModel> Options { get; }
}
