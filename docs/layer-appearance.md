# Layer Appearance

This document records the project direction for layer-based appearance.

Some parts are implemented today, while others are future design rules.

---

## Current implemented layer appearance

Currently layers control:

```text
Id
Name
Color
LineWeight
IsVisible
IsLocked
```

The Layer Manager v1 allows editing:

- name;
- visibility;
- locked state;
- color hex;
- line weight;
- current layer.

The current layer must remain visible and unlocked.

---

## Design rule: appearance belongs to layers

Long-term rule:

```text
entities store geometry + layer reference
layers store visual appearance
```

Individual entities should not carry their own color, line weight or fill color fields.

This keeps the entity model simpler and avoids ambiguity between ByLayer and explicit per-entity overrides.

---

## Future layer model

The intended complete layer model is:

```text
Id           unique identifier
Name         display name
Color        stroke color
LineWeight   stroke thickness
FillColor    fill color, nullable
DrawOrder    integer z-order
IsVisible    visibility toggle
IsLocked     lock toggle
```

`FillColor` and `DrawOrder` are not part of the current v1 Layer Manager implementation yet.

---

## Draw order

Future rule:

```text
lower DrawOrder  -> drawn first / background
higher DrawOrder -> drawn last / foreground
```

Within the same layer, entities should render in document insertion order.

There should be no per-entity z-order unless a future design decision explicitly changes this rule.

---

## Fill

Future fill behavior:

Only closed entities can be filled:

```text
CircleEntity
PolylineEntity with IsClosed = true
```

Open polylines and lines are never filled.

Fill is a layer attribute. Setting a layer fill color fills all fillable entities on that layer.

---

## Commands and undo

Layer appearance changes should go through commands.

Current implemented pattern:

```text
Layer Manager -> UpdateLayersCommand -> undoable layer update
```

Future fill color and draw order changes should reuse the same command-oriented approach.

---

## Persistence

Current persistence saves implemented layer fields.

When `FillColor` and `DrawOrder` are implemented, the serializer must be updated and the document format versioning strategy must be considered.
