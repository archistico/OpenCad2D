# Layers

Layers organize drawing entities and control visibility, locking, and default appearance. A well-structured drawing is much easier to edit because related objects can be shown, hidden, protected, or styled together.

Use layers deliberately. Avoid placing everything on one layer once the drawing becomes more than a quick sketch.

## Default layers

A new drawing should provide a small set of useful default layers. The current project baseline uses layers such as `0`, `Annotations`, `Walls`, `Axis`, and `Construction lines`.

The `0` layer is the general default. `Annotations` is suitable for dimensions and notes. `Walls` is intended for architectural geometry. `Axis` and `Construction lines` are useful for references, guides, and layout work.

## Current layer

New entities are usually created on the current layer. Before drawing a group of related objects, set the intended layer first. This is faster and cleaner than moving many entities afterward.

If an entity is created on the wrong layer, select it and change its layer from the Properties Panel.

## Visibility

Hidden layers are not displayed. They are useful when a drawing contains references, alternatives, imported content, or annotation groups that should be temporarily removed from view.

Hidden content should not interfere with normal selection or snapping. If the user cannot see it, it should not unexpectedly affect point picking.

## Locking

Locked layers protect their entities from accidental editing. This is useful for reference geometry, imported plans, attached images, construction backgrounds, and content that should remain visible but not editable.

A locked layer is different from a hidden layer. Locked content may still be visible, but it should not be modified through ordinary editing commands.

## Layer appearance

Layers can define appearance properties such as color, line weight, and line format. This makes it possible to keep drawing conventions consistent: walls can have a heavier continuous line, axes can use dash-dot, and construction geometry can use a lighter dashed style.

When an entity follows layer appearance, changing the layer updates the visual result. When an entity has an explicit override, the entity may keep its own appearance.

## Import behavior

When importing another drawing, compatible layers with the same name should be reused rather than duplicated unnecessarily. If two layers have the same name but different definitions, the application should resolve the conflict predictably.

This rule matters because repeated imports can otherwise create confusing layer lists. The final conflict behavior should remain documented in the import and technical reference pages as the implementation evolves.

## Visual assets to add

Layers should be documented with the layer list, the current layer selector, and a drawing where visibility or locking changes the result. Recommended assets are `docs/assets/images/layers/layer-manager-overview.png`, `docs/assets/gifs/snaps/layer-visibility-toggle.gif`, and `docs/assets/gifs/snaps/layer-lock-selection.gif`.
