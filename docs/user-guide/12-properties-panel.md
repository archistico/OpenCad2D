# Properties Panel

The Properties Panel displays information about the current selection and allows supported properties to be edited without starting a separate command. It is the main place to inspect what an entity is and how it is displayed.

The panel should stay practical. It should show the properties that matter for the selected object and avoid exposing internal implementation details that are not useful to the user.

## No selection

When nothing is selected, the panel can show general drawing information, current defaults, or remain mostly empty depending on the current interface state. The important point is that no entity-specific edit is being applied.

This state is also useful as a reset point. If the panel still appears to describe an object after clearing the selection, the user should verify whether something is still selected.

## Single selection

When one entity is selected, the panel can show both common properties and entity-specific properties. Common properties usually include layer, color or layer-based appearance, line format, line weight, and draw order. Entity-specific properties depend on the selected object.

A line may expose endpoints or length-related information. Text exposes text content and formatting. A dimension exposes style-related properties. An image exposes reference and display properties. A block exposes block-related information.

## Multiple selection

When several entities are selected, the panel should focus on properties that can be edited together. Layer, color, line format, and similar common properties are typical examples.

If the selected entities have different values, the panel should communicate that the value is mixed. When the user assigns a new value, it should apply to all selected entities that support that property.

## Layer and appearance changes

Changing the layer from the Properties Panel is often faster than using a dedicated command. Select the entities, choose the target layer, and the selected objects move to that layer.

Appearance may come directly from the entity or indirectly from the layer, depending on the current model. The documentation should describe the visible behavior rather than the internal storage details.

## Geometry and annotation properties

Some geometric properties may be editable directly, while others are better modified through grips or commands. For example, moving a line endpoint may be clearer with grips, while changing a text style may be clearer in the panel.

The same principle applies to dimensions. A dimension style should control the overall appearance, while individual properties should be used only when the specific dimension needs an exception.

## Related chapters

The Properties Panel is connected to several parts of the guide: [Layers](16-layers.md), [Line Formats](17-line-formats.md), [Text Formats](18-text-formats.md), and [Dimension Styles](19-dimension-styles.md).

## Visual assets to add

The Properties Panel should be shown in three states: no selection, single entity selection, and multiple selection. Recommended assets are `docs/assets/images/interface/properties-panel-empty.png`, `docs/assets/images/interface/properties-panel-line.png`, and `docs/assets/images/interface/properties-panel-multiple-selection.png`.
