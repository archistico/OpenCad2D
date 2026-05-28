using System.Collections.Generic;
using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.App.ViewModels.Library;

public sealed record LibraryBlockDefinitionPreparation(
    BlockDefinitionId BlockDefinitionId,
    string BlockName,
    BlockDefinition Definition,
    IReadOnlyList<ICadCommand> DefinitionCommands);
