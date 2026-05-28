# Library Browser

The Library Browser inserts reusable fixed drawing snippets stored as native `.opencad2d.json` files.

It is intended for furniture, standard symbols, sanitary fixtures, annotation marks and other fixed reusable drawings. Parametric objects that need user-provided dimensions should remain dedicated tools.

---

## Folder layout

Create a `library/` folder either in the project working directory or beside the built application. The browser scans recursively for `.opencad2d.json` files.

Recommended layout:

```text
library/
  arredo/
    chair.opencad2d.json
    table.opencad2d.json
  simboli/
    north-simple.opencad2d.json
  sanitari/
    wc.opencad2d.json
  porte-finestre/
    door-80.opencad2d.json
  annotazioni/
    section-arrow-a.opencad2d.json
```

The category is the first folder below `library/`. For example:

```text
library/arredo/sedie/chair.opencad2d.json
```

is shown in category `arredo`.

Files outside the `.opencad2d.json` extension are ignored. Invalid native files are skipped and reported as non-blocking warnings in the browser.

---

## Creating an item

1. Open OpenCad2D.
2. Draw the reusable item as normal CAD geometry.
3. Place the intended insertion base point at model origin `(0,0)`.
4. Save the drawing as `.opencad2d.json`.
5. Move or save the file under `library/<category>/`.

The browser uses the file name as the item name. For example:

```text
library/arredo/sedia.opencad2d.json
```

appears as item `sedia` in category `arredo`.

---

## Inserting an item

Workflow:

```text
Library -> category -> item -> preview -> Insert -> pick insertion point
```

Insertion uses the active canvas snaps. The file origin `(0,0)` becomes the picked insertion point.

The item is inserted as a block reference:

- a deterministic block definition is created from the library file;
- repeated insertions of the same item reuse the existing definition;
- the inserted reference can be selected, moved, copied, rotated, scaled and exploded like other block references;
- undo removes the inserted reference and, when the definition was created by that insertion, removes the definition in the same undo step.

---

## Preview

The browser shows a vector preview of the selected item. The preview uses the same entity renderer as the main canvas and fits the drawing into the preview area.

The light origin axes show where `(0,0)` is in the item. This is the point that will land on the insertion point when the item is inserted.

---

## Current limitations

The first Library Browser pass is intentionally conservative:

- no online library or package manager;
- no metadata manifest, tags or custom ordering yet;
- no thumbnail cache;
- no nested category tree in the UI beyond first-level category grouping;
- no scale/rotation options in the Library dialog;
- no automatic unit conversion;
- library items containing block references are rejected for now;
- fixed items are inserted as block references; use Explode when raw editable geometry is needed.

---

## Recommended item rules

- Keep fixed symbols near `(0,0)` so insertion is predictable.
- Prefer ordinary entities for first-pass library items.
- Use clear file names, because the file name is the displayed item name.
- Put variants in separate files instead of adding many toolbar buttons.
- Reserve direct tool buttons for parametric generators such as doors, windows, stairs, section markers and title blocks.
