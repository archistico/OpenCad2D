# Application Settings

This document covers four configuration areas: keyboard shortcuts, window and session persistence, grid improvements and drawing configuration.

---

## Keyboard Shortcuts

Keyboard shortcuts allow the user to activate tools and trigger commands without using the mouse.

### Design rule

Shortcut handling lives in `OpenCad2D.App`. It is not part of the tool system.

The shortcut map is a configuration that binds key combinations to actions. Actions are:

```text
tool activation    activate a specific tool
command execution  trigger a document or file command
toggle             flip a boolean mode (Ortho, snap, grid)
```

The shortcut system does not create entities directly. Activating a tool via shortcut is equivalent to clicking the tool button.

### Multi-character shortcuts

Some shortcuts use two-character codes that mirror common CAD conventions. A small input buffer collects characters and matches them against the shortcut map.

The buffer resets after a configurable short timeout (for example 1.5 seconds), or immediately after a match or a definitive non-match.

Single-character shortcuts match immediately without waiting for a second character.

Multi-character shortcuts are only active when no command line input is in progress.

### Default shortcut map

| Shortcut | Action |
|---|---|
| `Esc` | Cancel active operation / deselect all (second press) |
| `S` | Selection tool |
| `L` | Line tool |
| `R` | Rectangle tool |
| `C` | Circle tool |
| `T` | Text tool |
| `PO` | Polygon tool |
| `RO` | Rotate tool |
| `SC` | Scale tool |
| `AL` | Align tool |
| `M` | Move tool |
| `CO` | Copy tool |
| `DE` or `Delete` | Delete selected |
| `MA` | Match Properties tool |
| `DI` | Distance measure tool |
| `AR` | Area measure tool |
| `Tab` | Activate grip editing |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Ctrl+O` | Open |
| `Ctrl+N` | New |
| `Ctrl+A` | Select all |
| `F3` | Toggle object snapping |
| `F8` | Toggle Ortho mode |
| `G` | Toggle grid visibility |

### Configurable shortcuts

Shortcuts are stored in the user settings file, not in the document file. They are user-local and not shared with collaborators.

A future settings panel can allow the user to reassign shortcuts. Until that panel exists, defaults apply and are not exposed in the UI.

---

## Window and Session Persistence

The application saves and restores UI state between sessions so that the window opens in the same position and configuration as when it was last closed.

### Persisted state

On application exit, the following state is saved:

```text
main window position (screen X, Y)
main window size (Width, Height)
main window maximized state
last opened file path
```

On startup, the application reads the saved state and applies it before showing the window. If no saved state exists, the window opens at a reasonable default size and centered on the primary screen.

If the saved window position is off-screen (for example because a monitor was disconnected since the last session), the application resets the window to the default position.

### Storage location

Session state is stored in a user-local JSON file:

```text
Windows   %APPDATA%\OpenCad2D\settings.json
Linux     ~/.config/OpenCad2D/settings.json
macOS     ~/Library/Application Support/OpenCad2D/settings.json
```

This file is managed exclusively by `OpenCad2D.App`.

### Design rule

Session persistence must not involve `OpenCad2D.Persistence`.

The settings file and the document file serve different purposes:

```text
document file (.opencad2d.json)   drawing content, portable, shareable
settings file (settings.json)     user preferences, local, not shareable
```

The document serializer must not read or write any application UI state. The session file must not contain any drawing content.

---

## Grid Improvements

The visual grid supports two levels of resolution, zoom-based automatic visibility and a user toggle.

### Two grid levels

The grid has two independent levels:

**Primary grid:** major lines at a configurable step. These lines are bolder or more opaque. The primary step is typically a round number in document units (for example 100 mm or 1 m).

**Secondary grid:** minor lines that subdivide primary cells into equal intervals. For example, if the primary step is 100 and there are 10 subdivisions, secondary lines appear every 10 units.

Both steps are independently configurable. The secondary step must be a divisor of the primary step to produce aligned subdivisions.

### Zoom-based visibility

When the user zooms far out, grid lines become too dense to be readable or useful. Each grid level has a minimum pixel spacing threshold.

Rule:

```text
if the screen distance between two adjacent grid lines falls below MinPixelSpacing
-> that grid level is suppressed automatically
```

Typical values:

```text
MinPixelSpacing for secondary grid  around 8 pixels
MinPixelSpacing for primary grid    around 4 pixels
```

These thresholds prevent the canvas from becoming cluttered at small zoom levels.

When the user zooms back in, the suppressed level reappears automatically.

### Grid toggle

The grid is toggled by the `G` shortcut or by a button in the snap/Ortho bar.

Grid visibility is a UI display setting. It is stored in the session settings file, not in the drawing document. Hiding the grid does not affect grid snapping.

### GridSettings model

`GridSettings` extends to include:

```text
PrimaryStep          step of major grid lines (model units)
SecondaryDivisions   number of subdivisions within each primary cell
SnapStep             step used by grid snapping (may differ from visual grid)
IsVisible            whether the visual grid is shown
MinPixelSpacingPrimary     pixel threshold below which primary grid is hidden
MinPixelSpacingSecondary   pixel threshold below which secondary grid is hidden
```

### Grid snapping alignment

Grid snapping uses `SnapStep` from `GridSettings`. By default `SnapStep` equals `SecondaryStep` (primary step divided by secondary divisions). This can be configured separately.

The visual grid and the snap grid remain conceptually aligned. Showing the grid gives the user a clear reference for where snap points will be.

---

## Drawing Configuration

Drawing configuration stores document-level parameters that govern measurement units, display precision, dimension appearance and default tool behavior.

This is distinct from application settings, which are user-local and session-specific.

### DrawingSettings

`DrawingSettings` lives inside `CadDocument` and is serialized as part of the `.opencad2d.json` document format.

It contains:

```text
Units               measurement unit system (mm, cm, m, inch, feet)
LinearPrecision     decimal places for length display and dimension values
AngularPrecision    decimal places for angle display
DefaultDimensionStyleId   id of the style used for new dimensions
GridSettings        primary step, secondary divisions, snap step, visibility
DefaultSnapTolerance   model-unit tolerance for snapping
DefaultTextHeight   default height for new text entities
```

When a document is opened, `DrawingSettings` is loaded and applied. All tools, the status bar and dimension entities use these settings for formatting and defaults.

### Changes through commands

Changes to `DrawingSettings` go through `UpdateDrawingSettingsCommand`.

```text
Execute   apply new settings to the document
Undo      restore previous settings
```

This ensures that settings changes are undoable and that the command history generation counter updates, marking the document as dirty.

### Configuration UI

A settings dialog or panel exposes `DrawingSettings` for the current document.

The dialog is hosted by `OpenCad2D.App`. It reads `DrawingSettings` from the workspace, presents editable fields and submits `UpdateDrawingSettingsCommand` on confirmation.

Cancelling the dialog does not submit any command and leaves the document unchanged.

### Separation from session settings

Document settings and session settings serve different purposes and are stored separately:

```text
DrawingSettings (document)
  -> saved in .opencad2d.json
  -> shared with anyone who opens the file
  -> changing them marks the document dirty

Session settings (application)
  -> saved in settings.json
  -> user-local
  -> do not affect the drawing content
```

A document opened on a different machine uses the same `DrawingSettings`. A different machine uses its own session settings (window position, shortcuts and so on).
