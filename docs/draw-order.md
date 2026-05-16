# Draw order / Z-order

OpenCad2D keeps visual stacking independent from layers.

Layers control visibility, locking and default visual format. They do not define whether an entity is drawn above or below another entity. Stacking is controlled by each entity's `DrawOrder` value.

## Rules

- Lower `DrawOrder` values are drawn first.
- Higher `DrawOrder` values are drawn later and therefore appear on top.
- Hidden layers still hide their entities regardless of draw order.
- Locked layers still prevent selection/editing regardless of draw order.
- Hit-testing uses draw order as the topmost tie-breaker for overlapping entities.

## Commands

The current selection can be reordered with:

| Action | Aliases |
| --- | --- |
| Bring to Front | `BRINGTOFRONT`, `BTF`, `FRONT` |
| Send to Back | `SENDTOBACK`, `STB`, `BACK` |
| Bring Forward | `BRINGFORWARD`, `BF`, `FORWARD` |
| Send Backward | `SENDBACKWARD`, `SB`, `BACKWARD` |

The same actions are available from the left tool panel under the `ORDER` section.

## Behavior

- `To Front` assigns the selected entities a draw order above the current topmost entity.
- `To Back` assigns the selected entities a draw order below the current bottom entity.
- `Forward` moves selected entities above the next unselected entity when possible.
- `Backward` moves selected entities below the previous unselected entity when possible.
- Selection is preserved after reordering.
- All operations are undoable.


## Property panel

For a single selected entity, the Properties panel displays the entity `Draw order` value. The value is read-only for now; order changes should be made through the dedicated order commands (`To Front`, `To Back`, `Forward`, `Backward`) so changes stay normalized and undoable.

`To Front` assigns the selected entities the highest draw order range. Because visible entities are rendered in increasing draw order, entities moved to front are drawn after the others and therefore appear above them. Hit testing also prefers higher draw order entities when objects overlap.
