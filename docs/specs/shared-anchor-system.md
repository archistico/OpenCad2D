# Shared Specification — 9-Point Anchor System

## Purpose

Many upcoming OpenCad2D tools need the same concept of an insertion or reference anchor: parametric doors and windows, coordinate callouts, annotation bubbles, future dynamic symbols and possibly other reusable parametric objects. This document defines a shared 9-point anchor contract so every feature does not invent a different insertion convention.

Native blocks are the explicit exception to this contract. A block reference is always inserted by the base point chosen when the block definition is created. The 9-point anchor selector must not reinterpret existing block insertion, Library insertion or block reference placement unless a future specification deliberately introduces a separate, opt-in wrapper command.

The visual model follows the common design-tool convention used by applications such as Illustrator: the object is considered inside a bounding rectangle and the user chooses one of the corners, edge centers, or center point as the reference anchor.

## Anchor values

Use a single enum-like vocabulary across code, persistence, HUD, Property Panel and documentation:

```text
TopLeft       TopCenter       TopRight
MiddleLeft    Center          MiddleRight
BottomLeft    BottomCenter    BottomRight
```

The default should be `Center` for generic parametric or annotation tools unless a specific tool has a better domain default. Doors and windows may default to `Center` or to another canonical wall-facing anchor only if the behavior is documented in their own specification. Do not introduce synonyms such as `UpperLeft`, `Middle`, `Origin`, or `InsertionCorner` unless they are UI labels mapped explicitly to the canonical values above.

For blocks and Library items inserted as blocks, the effective insertion reference is not `Center` and not one of these 9 anchor values. It is the block definition base point/origin.

## Coordinate meaning

For tools that opt in, the anchor is a transformation reference, not merely a visual hint. When the user picks an insertion point, OpenCad2D must place the selected anchor at that point and derive the entity transform from there. Blocks do not opt in: their insertion point remains the block base point.

For a rectangular unrotated local bounding box:

| Anchor | Local reference point |
|---|---|
| `TopLeft` | minimum X, maximum Y |
| `TopCenter` | center X, maximum Y |
| `TopRight` | maximum X, maximum Y |
| `MiddleLeft` | minimum X, center Y |
| `Center` | center X, center Y |
| `MiddleRight` | maximum X, center Y |
| `BottomLeft` | minimum X, minimum Y |
| `BottomCenter` | center X, minimum Y |
| `BottomRight` | maximum X, minimum Y |

If the internal CAD Y-axis uses screen-inverted coordinates at any rendering layer, the document/model contract must still remain CAD-oriented. UI rendering can flip the visual representation, but persistence and geometry calculations must use the model coordinate convention consistently.

## Bounding box source

The anchor reference rectangle should come from the entity's local extents before world transform when possible. The local extents must be stable and independent from current zoom, screen DPI, selection handles or transient preview adorners.

Recommended sources for tools that opt in:

- door/window: parametric object local extents including the mask footprint if the mask changes insertion semantics;
- annotation bubble/callout: visible marker extents, excluding the leader unless the specific tool states otherwise;
- image reference: image local rectangle;
- generic selected entity set: union of selected entities in local or temporary group coordinates.

Do not use block definition extents as a replacement for the block base point in normal Insert Block or Library insertion. The base point is part of the block definition contract and is intentionally more precise than a derived bounding-box anchor.

If an entity cannot provide meaningful local extents, it should not expose the 9-point anchor selector until a stable bounding rule exists.

## HUD behavior

The Dynamic HUD is the primary interaction layer. The anchor selector must respect the existing HUD focus contract:

- the HUD displays the current anchor while the user is picking points;
- the selector is not mouse-editable until `TAB` enters HUD editing mode;
- keyboard options can open an anchor sub-prompt, for example `A` / `Anchor`;
- the anchor edit UI should show a compact 3x3 selector;
- changing the anchor must update the preview immediately but must not commit document geometry until the command is confirmed;
- `Enter` confirms the current HUD edit or command phase according to the active command state;
- `Esc` cancels the current edit/sub-prompt or command according to the existing command cancellation contract.

Suggested numeric shortcut mapping, if implemented:

```text
7 TopLeft       8 TopCenter       9 TopRight
4 MiddleLeft    5 Center          6 MiddleRight
1 BottomLeft    2 BottomCenter    3 BottomRight
```

This keypad mapping is optional, but if implemented it must be documented in the User Guide.

## Property Panel behavior

Entities that persist an anchor should expose it in the Property Panel using the same 9 canonical values. The Property Panel may use a dropdown or a compact 3x3 selector. Changing the anchor after creation should keep the entity's visible geometry in place unless the feature-specific specification says the insertion point is authoritative and geometry should move around it.

Recommended rule:

- for parametric doors/windows: changing anchor should preserve the selected wall/insertion reference and update the opening footprint preview;
- for annotation bubbles: changing anchor affects text/bubble attachment, not the measured target point.

Block references should not expose this property as a normal block insertion setting. Their editable insertion reference is the base point defined at Create Block time. To change that reference, the user must edit/recreate the block definition base point or use a future explicit block-base-point operation, not a generic 9-point anchor property.

If a tool chooses a different rule, its specification must explicitly state why.

## Persistence

Use a stable string value in `.opencad2d.json`, not an integer ordinal. Example:

```json
{
  "anchor": "Center"
}
```

Missing anchor values in older files should load with the feature default. Invalid anchor values should be recovered conservatively: use the default and record a diagnostic if the existing recovery/logging system supports it.

## Export behavior

Anchor data is usually not exported to DXF/SVG/PDF/PNG because it is an editing preference, not visible geometry. The exported visible geometry must reflect the chosen anchor transform.

If future DXF export maps parametric objects to custom extension data, that must be handled separately and must not be required for visible drawing correctness.

## Testing requirements

Every entity or command that adopts anchors should have tests for:

- default anchor behavior;
- each corner anchor at minimum;
- save/reopen of persisted anchor values;
- preview transform matches committed transform;
- Property Panel edit behavior;
- export-visible geometry unchanged except for expected placement.

Manual tests should verify that HUD focus does not get stolen by the anchor selector before `TAB` enters edit mode. Regression tests/checklists must also confirm that Insert Block and Library insertion continue to use the block base point and never shift to a derived bounding-box anchor accidentally.

---

## Implementation checkpoint — v0.8.180A

The first code foundation is tracked in `docs/specs/v0.8.180A-shared-anchor-foundation.md`.

This checkpoint introduces the core anchor model and resolver only:

- `OpenCad2D.Core.Anchors.AnchorPoint`;
- `OpenCad2D.Core.Anchors.AnchorPointDescriptor`;
- `OpenCad2D.Core.Anchors.AnchorPlacement`;
- `OpenCad2D.Core.Anchors.AnchorPointService`.

It does not add a visible HUD selector, Property Panel editor, entity persistence change or door/window geometry. Future features must use this shared service rather than duplicating the 9-point mapping or recomputing CAD-oriented anchor points independently.
