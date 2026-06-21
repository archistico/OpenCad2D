# Shared Specification — Preview, Commit, and Grouped Undo Workflow

## Purpose

Complex commands should not mutate the document while the user is still moving the mouse or editing HUD values. This document defines the shared preview/commit/undo contract for future tools such as Blocks v2 operations, parametric doors/windows, Array tools, HatchEntity creation, Arrow and annotation markers, Library insertion and any later parametric object workflow.

## Core rule

A complex command follows this sequence:

```text
Collect input -> Build preview -> Confirm -> Commit single undoable operation
```

The document model must change only at commit time. Preview geometry is transient and must be discarded when the command is cancelled, fails validation, or starts a different phase.

## Command phases

A command should make its active phase explicit. Typical phases are:

```text
Selecting sources
Picking first point
Picking second point
Editing option value
Previewing result
Confirming
Committed
Cancelled
Failed validation
```

The HUD prompt should reflect the current phase and show only fields/options that make sense in that phase. This avoids the previous problem where unrelated coordinate fields remained visible while editing scalar options.

## Preview behavior

Preview geometry should:

- use the same geometry generation path as the final commit whenever possible;
- respect current layer/style rules unless the command intentionally uses a preview-only style;
- update when mouse position, snap point, HUD scalar values or options change;
- never be selectable or persisted;
- never appear in undo history;
- be cleared on cancel, error or command switch.

Preview must be visually close enough to the committed result that the user can trust it. If a command cannot preview reliably, the specification for that command must say why.

## Validation behavior

Commands should validate before commit. Invalid input should not create partial geometry. Common invalid cases:

- empty source selection;
- zero-length vectors;
- non-positive counts, widths, spacings or pattern scales;
- unsupported source/path entity type;
- self-intersecting hatch loop if unsupported;
- missing block definition;
- invalid SVG icon asset;
- wall mask footprint that cannot be generated.

Failure should produce a clear user-facing message and should leave the document unchanged.

## Grouped undo

Commands that create or modify multiple entities as a single user action must commit one grouped undo operation.

Examples:

- `ARRAYRECT` creates 40 copies: one undo removes all 40 copies;
- inserting a door with a mask: one undo removes the door and its mask data;
- creating a hatch with outer and inner loops: one undo removes the hatch;
- importing a Library item as a block reference and possibly adding a block definition: one undo should remove the reference and any newly added unused definition if the command created it;
- Blocks v2 rename: one undo restores the old name and all references coherently.

If a grouped undo cannot cleanly revert secondary data, the command should not be implemented until the undo model can support it.

## Source entity policy

Commands must explicitly state whether source entities are preserved, modified, or consumed.

| Command family | First-version recommendation |
|---|---|
| Array tools | Preserve sources; create real copies; no associative array entity for v1. |
| Door/window insertion | Create a persistent parametric object; do not modify wall geometry in v1. |
| Hatch creation | Preserve boundary sources unless the user explicitly deletes them. |
| Library insertion | Preserve library file; create or reuse block definitions and references. |
| Arrow/callouts | Create annotation entities; do not affect target geometry. |

## HUD editing contract

The shared HUD rule remains mandatory:

- text boxes are not interactive until `TAB` enters edit mode;
- typing while editing a field must not be overwritten by mouse movement;
- `Enter` confirms the field/phase;
- right-click may confirm where current tools already support it;
- `Esc` cancels the current edit or command;
- sub-prompts such as `Gap`, `Anchor`, `Rows`, `Arrow size` show only their own editable field(s) unless the tool has a clear reason to show more.

## Automated test expectations

For every complex command, include tests for:

- preview state does not alter document entity count;
- commit alters the document as expected;
- cancel leaves the document unchanged;
- invalid input leaves the document unchanged;
- undo reverts the whole operation in one step;
- redo reapplies the whole operation if redo is supported;
- save/reopen preserves committed data, not transient preview data.

## Manual verification expectations

Manual checklists should include at least:

- command start with preselection and post-selection if supported;
- mouse movement while HUD is visible;
- `TAB`, edit, `Enter`, `Esc` sequences;
- snap and ortho interaction during preview;
- Undo/Redo immediately after commit;
- save/reopen and export-visible result.
