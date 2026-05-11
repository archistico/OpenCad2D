using System;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.App.Rendering;

public static class EntityScreenStyleResolver
{
    private static readonly CadColor SelectedColor = CadColor.FromRgb(0, 191, 255);

    public static EntityScreenStyle Resolve(
        CadDocument document,
        CadEntity entity,
        bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(entity);

        Layer layer = ResolveLayer(
            document,
            entity);

        LineFormat lineFormat = ResolveLineFormat(
            document,
            layer);

        CadColor color = isSelected
            ? SelectedColor
            : lineFormat.Color;

        return new EntityScreenStyle(
            color,
            Math.Max(0, lineFormat.LineWeight.Millimeters),
            lineFormat.LineStyle);
    }

    private static LineFormat ResolveLineFormat(
        CadDocument document,
        Layer layer)
    {
        if (document.LineFormats.TryGetById(
            layer.LineFormatId,
            out LineFormat? lineFormat) &&
            lineFormat is not null)
        {
            return lineFormat;
        }

        return LineFormatCollection.Default.GetById(LineFormatCollection.Default.All[0].Id);
    }

    private static Layer ResolveLayer(
        CadDocument document,
        CadEntity entity)
    {
        return document.Layers.TryGet(entity.LayerId, out Layer? layer) &&
            layer is not null
            ? layer
            : Layer.Default;
    }
}
