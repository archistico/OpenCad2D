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

        CadColor color = isSelected
            ? SelectedColor
            : ResolveColor(
                entity,
                layer);

        return new EntityScreenStyle(
            color,
            Math.Max(0, layer.LineWeight.Millimeters));
    }

    private static CadColor ResolveColor(
        CadEntity entity,
        Layer layer)
    {
        return entity.Style.Color.IsByLayer
            ? layer.Color
            : entity.Style.Color;
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
