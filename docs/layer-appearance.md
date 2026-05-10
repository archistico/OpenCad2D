# Layer Appearance

Color, line weight and fill color are defined exclusively at the layer level.

Individual entities do not carry their own appearance properties. There is no per-entity color, no per-entity line weight and no ByLayer/explicit ambiguity. Every visual property is inherited from the layer the entity belongs to.

This is a deliberate design choice. It keeps the entity model simple, makes layer changes visually immediate and eliminates the class of bugs that arise when entity-level overrides and layer-level settings conflict.

---

## Design rule

`CadEntity` does not have a `Color`, `LineWeight` or `FillColor` field.

`CadLayer` owns:

```text
Color        ARGB color used for stroke rendering
LineWeight   line thickness in model units
FillColor    nullable ARGB color used for fill rendering
```

The rendering path always reads appearance from the entity's layer. There is no fallback to a per-entity value because no per-entity value exists.

When a layer color or line weight changes, all entities on that layer reflect the change immediately at the next render pass. No entity-level update is required.

---

## Layer draw order

Layers have an explicit draw order that controls which entities appear in front of which.

Each layer stores an integer `DrawOrder`. Layers with a lower value are drawn first and appear in the background. Layers with a higher value are drawn later and appear in the foreground.

Rules:

```text
lower DrawOrder  -> background (drawn first)
higher DrawOrder -> foreground (drawn last, on top)
```

Within the same layer, entities are drawn in document insertion order. The first inserted entity is drawn first and may be covered by later entities on the same layer.

There is no per-entity z-order. Draw order is a layer concept only.

### Rendering pass

The rendering pass must:

1. Collect all visible layers.
2. Sort them by `DrawOrder` ascending.
3. For each layer in that order, render its visible entities in document insertion order.

This replaces any position-based or insertion-order-based z-ordering at the document level.

### Reordering layers

The UI must expose a way to change layer draw order. Reordering a layer changes its `DrawOrder` value. Other layers may need their `DrawOrder` values adjusted to maintain a consistent sequence.

Layer reorder operations should go through a command so they are undoable.

---

## Fill

Closed entities on a layer with a non-null `FillColor` are rendered with a solid fill.

### Fillable entities

Only geometrically closed entities can be filled:

```text
PolylineEntity with IsClosed = true  -> filled
CircleEntity                         -> filled
```

Open polylines and line entities are never filled, even if their layer has a fill color.

### Fill is a layer attribute

`CadEntity` does not have a fill mode or fill color. The decision to fill is entirely driven by whether the entity's layer has a `FillColor` set.

Setting `FillColor` to null on a layer disables fill for all entities on that layer.

### Rendering order within an entity

For each filled entity, the rendering order is:

```text
1. render fill polygon
2. render stroke on top
```

The stroke is always rendered on top of the fill for the same entity.

### No patterns or gradients

Only solid fill is supported. Hatching, patterns, textures and gradients are out of scope at this stage.

---

## Layer model summary

The complete set of layer properties is:

```text
Id           unique identifier
Name         display name
Color        stroke color (ARGB)
LineWeight   stroke thickness
FillColor    fill color (ARGB, nullable)
DrawOrder    integer z-order, lower = background
IsVisible    visibility toggle
IsLocked     lock toggle
```

All appearance decisions for entities on this layer are derived from these fields. Entities themselves carry only geometric data and a layer reference.

---

## Implications for commands

When a command changes an entity's layer (such as `MatchLayerCommand`), the entity's visual appearance changes automatically at the next render. No other update is needed.

When a command modifies a layer's `Color`, `LineWeight` or `FillColor`, all entities on that layer update visually. The entities themselves are not modified.

Layer appearance changes should go through commands so they can be undone.

---

## Implications for persistence

The serializer must save all layer appearance fields:

```text
color
line weight
fill color (as null or ARGB value)
draw order
```

Entities do not serialize any appearance data. Their visual representation is fully reconstructed from the layer at load time.

---

## Current implementation status

Layer Manager v1 currently implements the practical subset of this design:

```text
Name
Color
LineWeight
IsVisible
IsLocked
Current layer selection
```

Layer appearance is already layer-owned for color and line weight. Entities do not carry per-entity color or per-entity line weight.

The following design goals are not implemented yet and remain future work:

```text
FillColor
DrawOrder
layer reorder UI
filled rendering for closed entities
serializer versioning for new layer appearance fields
```

Layer Manager v1 applies confirmed changes through `UpdateLayersCommand`, making layer updates undoable and dirty-state aware.

Current Layer Manager rules:

```text
layer 0 cannot be deleted or renamed
current layer cannot be deleted
layers containing entities cannot be deleted
layer names must be unique and non-empty
current layer must be visible and unlocked
```
