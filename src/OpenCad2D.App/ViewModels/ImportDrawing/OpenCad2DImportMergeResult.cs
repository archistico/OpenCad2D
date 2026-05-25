using System;
using System.Collections.Generic;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;

namespace OpenCad2D.App.ViewModels.ImportDrawing;

public sealed class OpenCad2DImportMergeResult
{
    public OpenCad2DImportMergeResult(
        ICadCommand command,
        IReadOnlyList<CadEntity> importedEntities,
        int addedLineFormatCount,
        int addedTextFormatCount,
        int addedDimensionStyleCount,
        int addedLayerCount)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(importedEntities);

        Command = command;
        ImportedEntities = importedEntities;
        AddedLineFormatCount = addedLineFormatCount;
        AddedTextFormatCount = addedTextFormatCount;
        AddedDimensionStyleCount = addedDimensionStyleCount;
        AddedLayerCount = addedLayerCount;
    }

    public ICadCommand Command { get; }

    public IReadOnlyList<CadEntity> ImportedEntities { get; }

    public int ImportedEntityCount => ImportedEntities.Count;

    public int AddedLineFormatCount { get; }

    public int AddedTextFormatCount { get; }

    public int AddedDimensionStyleCount { get; }

    public int AddedLayerCount { get; }
}
