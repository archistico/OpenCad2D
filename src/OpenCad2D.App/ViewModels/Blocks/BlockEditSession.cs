using System.Collections.Generic;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.App.ViewModels.Blocks;

public sealed record BlockEditSession(
    BlockDefinitionId BlockDefinitionId,
    string BlockName,
    BlockReferenceEntity OriginalReference,
    IReadOnlyList<EntityId> EditEntityIds);
