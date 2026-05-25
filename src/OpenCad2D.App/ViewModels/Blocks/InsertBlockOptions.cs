using OpenCad2D.Core.Identifiers;
using System;

namespace OpenCad2D.App.ViewModels.Blocks;

public sealed class InsertBlockOptions
{
    public InsertBlockOptions(
        BlockDefinitionId blockDefinitionId,
        string blockName,
        double scale,
        double rotationDegrees)
    {
        if (!double.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scale),
                scale,
                "Block insertion scale must be a positive finite value.");
        }

        if (!double.IsFinite(rotationDegrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationDegrees),
                rotationDegrees,
                "Block insertion rotation must be a finite value.");
        }

        BlockDefinitionId = blockDefinitionId;
        BlockName = string.IsNullOrWhiteSpace(blockName)
            ? "Block"
            : blockName.Trim();
        Scale = scale;
        RotationDegrees = rotationDegrees;
    }

    public BlockDefinitionId BlockDefinitionId { get; }

    public string BlockName { get; }

    public double Scale { get; }

    public double RotationDegrees { get; }
}
