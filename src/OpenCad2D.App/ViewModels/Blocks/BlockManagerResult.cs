using System.Collections.Generic;
using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.App.ViewModels.Blocks;

public sealed record BlockManagerResult(
    BlockManagerAction Action,
    IReadOnlyList<BlockDefinition> BlockDefinitions,
    BlockDefinitionId? SelectedBlockDefinitionId,
    string? SelectedBlockName);
