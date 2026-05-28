using System;
using System.Collections.Generic;
using System.Linq;
using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.App.ViewModels.Library;

public sealed class LibraryBlockDefinitionBuilder
{
    public LibraryBlockDefinitionPreparation Prepare(
        CadDocument target,
        CadDocument source,
        LibraryCatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(item);

        BlockDefinitionId blockDefinitionId = new($"Library.{item.Id}");
        string blockName = BuildBlockName(item);

        if (target.BlockDefinitions.TryGet(blockDefinitionId, out BlockDefinition? existingDefinition) &&
            existingDefinition is not null)
        {
            return new LibraryBlockDefinitionPreparation(
                existingDefinition.Id,
                existingDefinition.Name,
                existingDefinition,
                Array.Empty<ICadCommand>());
        }

        if (source.Entities.All.Count == 0)
        {
            throw new InvalidOperationException("The selected library item contains no insertable entities.");
        }

        if (source.BlockDefinitions.Count > 0 ||
            source.Entities.All.OfType<BlockReferenceEntity>().Any())
        {
            throw new InvalidOperationException(
                "Library items containing block references are not supported by the first library insertion pass.");
        }

        var lineFormatIdMap = new Dictionary<LineFormatId, LineFormatId>();
        var textFormatIdMap = new Dictionary<TextFormatId, TextFormatId>();
        var dimensionStyleIdMap = new Dictionary<DimensionStyleId, DimensionStyleId>();
        var layerIdMap = new Dictionary<LayerId, LayerId>();

        IReadOnlyList<LineFormat> mergedLineFormats = MergeLineFormats(
            target,
            source,
            lineFormatIdMap,
            out int addedLineFormatCount);

        IReadOnlyList<TextFormat> mergedTextFormats = MergeTextFormats(
            target,
            source,
            textFormatIdMap,
            out int addedTextFormatCount);

        IReadOnlyList<DimensionStyle> mergedDimensionStyles = MergeDimensionStyles(
            target,
            source,
            textFormatIdMap,
            dimensionStyleIdMap,
            out int addedDimensionStyleCount);

        IReadOnlyList<Layer> mergedLayers = MergeLayers(
            target,
            source,
            lineFormatIdMap,
            layerIdMap,
            out int addedLayerCount);

        IReadOnlyList<CadEntity> blockEntities = source.Entities.All
            .Select(entity => RemapEntity(
                entity,
                layerIdMap,
                textFormatIdMap,
                dimensionStyleIdMap))
            .Select(entity => entity.WithId(EntityId.New()))
            .ToList();

        if (target.BlockDefinitions.ContainsName(blockName))
        {
            blockName = CreateUniqueName(
                blockName,
                target.BlockDefinitions.All.Select(definition => definition.Name));
        }

        var blockDefinition = new BlockDefinition(
            blockDefinitionId,
            blockName,
            blockEntities);

        var commands = new List<ICadCommand>();

        if (addedLineFormatCount > 0)
        {
            commands.Add(new UpdateLineFormatsCommand(
                target.LineFormats.All,
                mergedLineFormats));
        }

        if (addedTextFormatCount > 0)
        {
            commands.Add(new UpdateTextFormatsCommand(
                target.TextFormats.All,
                mergedTextFormats));
        }

        if (addedDimensionStyleCount > 0)
        {
            commands.Add(new UpdateDimensionStylesCommand(
                target.DimensionStyles.All,
                mergedDimensionStyles,
                target.CurrentDimensionStyleId,
                target.CurrentDimensionStyleId));
        }

        if (addedLayerCount > 0)
        {
            commands.Add(new UpdateLayersCommand(
                target.Layers.All,
                mergedLayers));
        }

        commands.Add(new AddBlockDefinitionCommand(blockDefinition));

        return new LibraryBlockDefinitionPreparation(
            blockDefinition.Id,
            blockDefinition.Name,
            blockDefinition,
            commands);
    }

    private static string BuildBlockName(LibraryCatalogItem item)
    {
        return $"Library/{item.Category}/{item.Title}";
    }

    private static IReadOnlyList<LineFormat> MergeLineFormats(
        CadDocument target,
        CadDocument source,
        Dictionary<LineFormatId, LineFormatId> idMap,
        out int addedCount)
    {
        List<LineFormat> result = target.LineFormats.All.ToList();
        addedCount = 0;

        foreach (LineFormat sourceFormat in source.LineFormats.All)
        {
            if (target.LineFormats.TryGetById(sourceFormat.Id, out LineFormat? existingFormat) &&
                existingFormat is not null &&
                AreEquivalent(existingFormat, sourceFormat))
            {
                idMap[sourceFormat.Id] = sourceFormat.Id;
                continue;
            }

            LineFormatId targetId = target.LineFormats.Contains(sourceFormat.Id)
                ? new LineFormatId(CreateUniqueId(sourceFormat.Id.Value, result.Select(format => format.Id.Value)))
                : sourceFormat.Id;
            string targetName = sourceFormat.Name;

            if (ContainsName(result.Select(format => format.Name), targetName))
            {
                targetName = CreateUniqueName(targetName, result.Select(format => format.Name));
                targetId = new LineFormatId(CreateUniqueId(targetId.Value, result.Select(format => format.Id.Value)));
            }

            result.Add(new LineFormat(
                targetId,
                targetName,
                sourceFormat.Color,
                sourceFormat.LineWeight,
                sourceFormat.LineStyle,
                sourceFormat.DashPattern));

            idMap[sourceFormat.Id] = targetId;
            addedCount++;
        }

        return result;
    }

    private static IReadOnlyList<TextFormat> MergeTextFormats(
        CadDocument target,
        CadDocument source,
        Dictionary<TextFormatId, TextFormatId> idMap,
        out int addedCount)
    {
        List<TextFormat> result = target.TextFormats.All.ToList();
        addedCount = 0;

        foreach (TextFormat sourceFormat in source.TextFormats.All)
        {
            if (target.TextFormats.TryGetById(sourceFormat.Id, out TextFormat? existingFormat) &&
                existingFormat is not null &&
                AreEquivalent(existingFormat, sourceFormat))
            {
                idMap[sourceFormat.Id] = sourceFormat.Id;
                continue;
            }

            TextFormatId targetId = target.TextFormats.Contains(sourceFormat.Id)
                ? new TextFormatId(CreateUniqueId(sourceFormat.Id.Value, result.Select(format => format.Id.Value)))
                : sourceFormat.Id;
            string targetName = sourceFormat.Name;

            if (ContainsName(result.Select(format => format.Name), targetName))
            {
                targetName = CreateUniqueName(targetName, result.Select(format => format.Name));
                targetId = new TextFormatId(CreateUniqueId(targetId.Value, result.Select(format => format.Id.Value)));
            }

            result.Add(new TextFormat(
                targetId,
                targetName,
                sourceFormat.FontFamily,
                sourceFormat.Height,
                sourceFormat.Color,
                sourceFormat.IsBold,
                sourceFormat.IsItalic));

            idMap[sourceFormat.Id] = targetId;
            addedCount++;
        }

        return result;
    }

    private static IReadOnlyList<DimensionStyle> MergeDimensionStyles(
        CadDocument target,
        CadDocument source,
        IReadOnlyDictionary<TextFormatId, TextFormatId> textFormatIdMap,
        Dictionary<DimensionStyleId, DimensionStyleId> idMap,
        out int addedCount)
    {
        List<DimensionStyle> result = target.DimensionStyles.All.ToList();
        addedCount = 0;

        foreach (DimensionStyle sourceStyle in source.DimensionStyles.All)
        {
            TextFormatId targetTextFormatId = textFormatIdMap.TryGetValue(sourceStyle.TextFormatId, out TextFormatId remappedTextFormatId)
                ? remappedTextFormatId
                : TextFormatId.Standard;

            if (target.DimensionStyles.TryGetById(sourceStyle.Id, out DimensionStyle? existingStyle) &&
                existingStyle is not null &&
                AreEquivalent(existingStyle, sourceStyle, targetTextFormatId))
            {
                idMap[sourceStyle.Id] = sourceStyle.Id;
                continue;
            }

            DimensionStyleId targetId = target.DimensionStyles.Contains(sourceStyle.Id)
                ? new DimensionStyleId(CreateUniqueId(sourceStyle.Id.Value, result.Select(style => style.Id.Value)))
                : sourceStyle.Id;
            string targetName = sourceStyle.Name;

            if (ContainsName(result.Select(style => style.Name), targetName))
            {
                targetName = CreateUniqueName(targetName, result.Select(style => style.Name));
                targetId = new DimensionStyleId(CreateUniqueId(targetId.Value, result.Select(style => style.Id.Value)));
            }

            result.Add(new DimensionStyle(
                targetId,
                targetName,
                targetTextFormatId,
                sourceStyle.ArrowSize,
                sourceStyle.TextOffset,
                sourceStyle.ExtensionLineOffset,
                sourceStyle.ExtensionLineOvershoot,
                sourceStyle.DecimalPlaces,
                sourceStyle.DecimalSeparator,
                sourceStyle.Suffix,
                sourceStyle.Prefix,
                sourceStyle.RadiusPrefix,
                sourceStyle.DiameterPrefix,
                sourceStyle.ArrowSymbol,
                sourceStyle.TextRotationMode,
                sourceStyle.DimensionLineOffset,
                sourceStyle.TextFitMode,
                sourceStyle.TerminatorFitMode));

            idMap[sourceStyle.Id] = targetId;
            addedCount++;
        }

        return result;
    }

    private static IReadOnlyList<Layer> MergeLayers(
        CadDocument target,
        CadDocument source,
        IReadOnlyDictionary<LineFormatId, LineFormatId> lineFormatIdMap,
        Dictionary<LayerId, LayerId> idMap,
        out int addedCount)
    {
        List<Layer> result = target.Layers.All.ToList();
        addedCount = 0;

        foreach (Layer sourceLayer in source.Layers.All)
        {
            LineFormatId targetLineFormatId = lineFormatIdMap.TryGetValue(sourceLayer.LineFormatId, out LineFormatId remappedLineFormatId)
                ? remappedLineFormatId
                : LineFormatId.Continuous;

            if (target.Layers.TryGet(sourceLayer.Id, out Layer? existingLayer) &&
                existingLayer is not null &&
                AreEquivalent(existingLayer, sourceLayer, targetLineFormatId))
            {
                idMap[sourceLayer.Id] = sourceLayer.Id;
                continue;
            }

            LayerId targetId = target.Layers.Contains(sourceLayer.Id)
                ? new LayerId(CreateUniqueId(sourceLayer.Id.Value, result.Select(layer => layer.Id.Value)))
                : sourceLayer.Id;
            string targetName = sourceLayer.Name;

            if (ContainsName(result.Select(layer => layer.Name), targetName))
            {
                targetName = CreateUniqueName(targetName, result.Select(layer => layer.Name));
                targetId = new LayerId(CreateUniqueId(targetId.Value, result.Select(layer => layer.Id.Value)));
            }

            result.Add(new Layer(
                targetId,
                targetName,
                targetLineFormatId,
                sourceLayer.IsVisible,
                sourceLayer.IsLocked,
                sourceLayer.FillColor));

            idMap[sourceLayer.Id] = targetId;
            addedCount++;
        }

        return result;
    }

    private static CadEntity RemapEntity(
        CadEntity entity,
        IReadOnlyDictionary<LayerId, LayerId> layerIdMap,
        IReadOnlyDictionary<TextFormatId, TextFormatId> textFormatIdMap,
        IReadOnlyDictionary<DimensionStyleId, DimensionStyleId> dimensionStyleIdMap)
    {
        LayerId layerId = layerIdMap.TryGetValue(entity.LayerId, out LayerId remappedLayerId)
            ? remappedLayerId
            : LayerId.Default;

        CadEntity remapped = entity.WithLayer(layerId);

        return remapped switch
        {
            TextEntity text when textFormatIdMap.TryGetValue(text.TextFormatId, out TextFormatId textFormatId) =>
                text.WithTextFormat(textFormatId).WithLayer(layerId),
            MultilineTextEntity multilineText when textFormatIdMap.TryGetValue(multilineText.TextFormatId, out TextFormatId textFormatId) =>
                multilineText.WithTextFormat(textFormatId).WithLayer(layerId),
            LinearDimensionEntity linearDimension => RemapLinearDimension(linearDimension, layerId, dimensionStyleIdMap),
            AlignedDimensionEntity alignedDimension => RemapAlignedDimension(alignedDimension, layerId, dimensionStyleIdMap),
            RadiusDimensionEntity radiusDimension => RemapRadiusDimension(radiusDimension, layerId, dimensionStyleIdMap),
            DiameterDimensionEntity diameterDimension => RemapDiameterDimension(diameterDimension, layerId, dimensionStyleIdMap),
            AngularDimensionEntity angularDimension => RemapAngularDimension(angularDimension, layerId, dimensionStyleIdMap),
            _ => remapped
        };
    }

    private static DimensionStyleId RemapDimensionStyleId(
        DimensionEntity dimension,
        IReadOnlyDictionary<DimensionStyleId, DimensionStyleId> dimensionStyleIdMap)
    {
        return dimensionStyleIdMap.TryGetValue(dimension.DimensionStyleId, out DimensionStyleId remappedStyleId)
            ? remappedStyleId
            : DimensionStyleId.Standard;
    }

    private static CadEntity RemapLinearDimension(
        LinearDimensionEntity dimension,
        LayerId layerId,
        IReadOnlyDictionary<DimensionStyleId, DimensionStyleId> dimensionStyleIdMap)
    {
        return new LinearDimensionEntity(
            dimension.FirstPoint,
            dimension.SecondPoint,
            dimension.DimensionLinePoint,
            dimension.Orientation,
            RemapDimensionStyleId(dimension, dimensionStyleIdMap),
            dimension.TextOverride,
            dimension.Id,
            layerId,
            dimension.Style,
            dimension.IsVisible,
            dimension.IsLocked,
            dimension.DrawOrder,
            dimension.IsStale);
    }

    private static CadEntity RemapAlignedDimension(
        AlignedDimensionEntity dimension,
        LayerId layerId,
        IReadOnlyDictionary<DimensionStyleId, DimensionStyleId> dimensionStyleIdMap)
    {
        return new AlignedDimensionEntity(
            dimension.FirstPoint,
            dimension.SecondPoint,
            dimension.DimensionLinePoint,
            RemapDimensionStyleId(dimension, dimensionStyleIdMap),
            dimension.TextOverride,
            dimension.Id,
            layerId,
            dimension.Style,
            dimension.IsVisible,
            dimension.IsLocked,
            dimension.DrawOrder,
            dimension.IsStale);
    }

    private static CadEntity RemapRadiusDimension(
        RadiusDimensionEntity dimension,
        LayerId layerId,
        IReadOnlyDictionary<DimensionStyleId, DimensionStyleId> dimensionStyleIdMap)
    {
        return new RadiusDimensionEntity(
            dimension.Center,
            dimension.PointOnCircle,
            dimension.TextPoint,
            RemapDimensionStyleId(dimension, dimensionStyleIdMap),
            dimension.TextOverride,
            dimension.Id,
            layerId,
            dimension.Style,
            dimension.IsVisible,
            dimension.IsLocked,
            dimension.DrawOrder,
            dimension.IsStale);
    }

    private static CadEntity RemapDiameterDimension(
        DiameterDimensionEntity dimension,
        LayerId layerId,
        IReadOnlyDictionary<DimensionStyleId, DimensionStyleId> dimensionStyleIdMap)
    {
        return new DiameterDimensionEntity(
            dimension.Center,
            dimension.PointOnCircle,
            dimension.TextPoint,
            RemapDimensionStyleId(dimension, dimensionStyleIdMap),
            dimension.TextOverride,
            dimension.Id,
            layerId,
            dimension.Style,
            dimension.IsVisible,
            dimension.IsLocked,
            dimension.DrawOrder,
            dimension.IsStale);
    }

    private static CadEntity RemapAngularDimension(
        AngularDimensionEntity dimension,
        LayerId layerId,
        IReadOnlyDictionary<DimensionStyleId, DimensionStyleId> dimensionStyleIdMap)
    {
        return new AngularDimensionEntity(
            dimension.Center,
            dimension.FirstRayPoint,
            dimension.SecondRayPoint,
            dimension.ArcPoint,
            dimension.IsCounterClockwise,
            RemapDimensionStyleId(dimension, dimensionStyleIdMap),
            dimension.TextOverride,
            dimension.Id,
            layerId,
            dimension.Style,
            dimension.IsVisible,
            dimension.IsLocked,
            dimension.DrawOrder,
            dimension.IsStale);
    }

    private static bool AreEquivalent(
        LineFormat first,
        LineFormat second)
    {
        return string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase) &&
            first.Color == second.Color &&
            first.LineWeight == second.LineWeight &&
            first.LineStyle == second.LineStyle &&
            first.DashPattern.SequenceEqual(second.DashPattern);
    }

    private static bool AreEquivalent(
        TextFormat first,
        TextFormat second)
    {
        return string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(first.FontFamily, second.FontFamily, StringComparison.OrdinalIgnoreCase) &&
            AreNearlyEqual(first.Height, second.Height) &&
            first.Color == second.Color &&
            first.IsBold == second.IsBold &&
            first.IsItalic == second.IsItalic;
    }

    private static bool AreEquivalent(
        DimensionStyle first,
        DimensionStyle second,
        TextFormatId remappedSecondTextFormatId)
    {
        return string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase) &&
            first.TextFormatId == remappedSecondTextFormatId &&
            AreNearlyEqual(first.ArrowSize, second.ArrowSize) &&
            AreNearlyEqual(first.TextOffset, second.TextOffset) &&
            AreNearlyEqual(first.ExtensionLineOffset, second.ExtensionLineOffset) &&
            AreNearlyEqual(first.ExtensionLineOvershoot, second.ExtensionLineOvershoot) &&
            first.DecimalPlaces == second.DecimalPlaces &&
            first.DecimalSeparator == second.DecimalSeparator &&
            first.Prefix == second.Prefix &&
            first.Suffix == second.Suffix &&
            first.RadiusPrefix == second.RadiusPrefix &&
            first.DiameterPrefix == second.DiameterPrefix &&
            first.ArrowSymbol == second.ArrowSymbol &&
            first.TextRotationMode == second.TextRotationMode &&
            AreNearlyEqual(first.DimensionLineOffset, second.DimensionLineOffset) &&
            first.TextFitMode == second.TextFitMode &&
            first.TerminatorFitMode == second.TerminatorFitMode;
    }

    private static bool AreEquivalent(
        Layer first,
        Layer second,
        LineFormatId remappedSecondLineFormatId)
    {
        return string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase) &&
            first.LineFormatId == remappedSecondLineFormatId &&
            first.FillColor == second.FillColor &&
            first.IsVisible == second.IsVisible &&
            first.IsLocked == second.IsLocked;
    }

    private static bool AreNearlyEqual(
        double first,
        double second)
    {
        return Math.Abs(first - second) <= 1e-9;
    }

    private static bool ContainsName(
        IEnumerable<string> names,
        string name)
    {
        return names.Any(existingName => string.Equals(
            existingName.Trim(),
            name.Trim(),
            StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateUniqueName(
        string baseName,
        IEnumerable<string> existingNames)
    {
        HashSet<string> usedNames = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        string normalizedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "Library Item"
            : baseName.Trim();

        for (int index = 2; ; index++)
        {
            string candidate = $"{normalizedBaseName} ({index})";

            if (!usedNames.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private static string CreateUniqueId(
        string baseId,
        IEnumerable<string> existingIds)
    {
        HashSet<string> usedIds = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        string normalizedBaseId = string.IsNullOrWhiteSpace(baseId)
            ? "LibraryItem"
            : baseId.Trim();

        for (int index = 2; ; index++)
        {
            string candidate = $"{normalizedBaseId}_{index}";

            if (!usedIds.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
