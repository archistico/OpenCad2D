# Persistence

OpenCad2D uses an internal JSON persistence format for save and reopen.

---

## Format

File extension:

```text
.opencad2d.json
```

The format is intended for OpenCad2D project files, not for interoperability with other CAD applications.

---

## Project

Persistence lives in:

```text
src/OpenCad2D.Persistence
```

Dependency rule:

```text
OpenCad2D.Persistence -> OpenCad2D.Core -> OpenCad2D.Geometry
```

Persistence must not depend on:

```text
OpenCad2D.App
OpenCad2D.Tools
OpenCad2D.Interaction
```

---

## Responsibilities

The serializer handles:

- document version;
- layers;
- entities;
- current layer id;
- viewport state;
- unknown entity type handling;
- JSON file I/O;
- load/save exceptions.

The App handles:

- file dialogs;
- New/Open/Save/Save As;
- dirty state;
- Save changes confirmation;
- applying viewport state after load.

---

## Dirty state

Dirty state is tracked from command history generation.

After save/load/new:

```text
MarkSaved()
```

After a document command executes, undo or redo changes generation and the workspace can report dirty state.

---

## UI rule

File commands live in a stable top file command bar:

```text
New | Open | Save | Save As | current file name | dirty marker
```

Do not place file commands inside tool-specific UI areas.

---

## Save changes dialog

Before New, Open or window close:

```text
if IsDirty == false:
    continue
else:
    ask Save / Don't Save / Cancel
```

Behavior:

```text
Save       -> save first, then continue
Don't Save -> continue without saving
Cancel     -> abort operation
```

---

## Viewport state

Viewport pan and zoom are saved with the drawing and restored after loading.

Viewport state is consumed by the App layer. It is not CAD entity geometry.

---

## Persistence vs export

Persistence and export are intentionally separate.

```text
Save / Save As  -> writes .opencad2d.json and marks the document clean
Export SVG      -> writes .svg and leaves the document state unchanged
```

SVG export does not:

- change `CurrentFilePath`;
- call `MarkSaved()`;
- clear the dirty marker;
- participate in native document loading.

SVG export belongs to `OpenCad2D.Export`, while native document save/load belongs to `OpenCad2D.Persistence`.
