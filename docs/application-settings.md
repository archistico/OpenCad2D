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
toggle             flip a boolean mode (Ortho, snap, grid) or change Polar Tracking option
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
| `Esc` | Cancel active operation / return to Selection / clear selection on second press |
| `S` | Selection tool |
| `L` | Line tool |
| `R` | Rectangle tool |
| `RS` | Rectangle by sides tool |
| `C` | Circle tool |
| `A` | Arc tool |
| `A3` | Arc 3 points tool |
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

## Polar Tracking UI

Polar Tracking is currently a runtime/session drawing aid exposed in the top CAD bar as `Polar:`.

Available options:

```text
Off
90°
45°
30°
15°
```

The selected option updates `CadWorkspace.AngleConstraintSettings` and `ToolContext.AngleConstraintSettings`.

Behavior:

```text
Off  -> no polar angular constraint
90°  -> directions at 0°, 90°, 180°, 270°
45°  -> directions at 0°, 45°, 90°, 135°, ...
30°  -> directions at 0°, 30°, 60°, 90°, ...
15°  -> directions at 0°, 15°, 30°, 45°, ...
```

Polar Tracking is not stored in the drawing file. It is a user/session aid, like snap toggles and viewport interaction state.

When Polar Tracking is enabled, it has priority over legacy Ortho. If Polar Tracking is `Off`, legacy Ortho can still provide horizontal/vertical constraint.

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

Grid configuration is managed through the `Grid...` button in the top CAD bar, near `Layers...`. The older inline grid controls should not be reintroduced in the menu/toolbar unless they are only quick toggles. Full grid editing belongs in the dedicated dialog.

The visual grid supports rectangular and isometric layouts, two visual resolutions, configurable origin and zoom-based visibility thresholds.

### Grid dialog

The current `Grid Settings` dialog edits:

```text
Show grid
Type: Rectangular / Isometric
Minor step
Major step
Origin X
Origin Y
Minimum screen spacing
Maximum screen spacing
Isometric diagonal angle
```

`Cancel` leaves the current settings unchanged. `OK` validates the values and applies the resulting `GridSettings` to the workspace.

Grid snap is still controlled separately by `SnapKind.Grid` from the snap bar. Showing or hiding the grid does not automatically enable or disable grid snapping.

### Rectangular grid

The rectangular grid is the standard orthogonal layout:

```text
horizontal lines
vertical lines
minor spacing = MinorStep
major spacing = MajorStep
```

Major lines are rendered more prominently than minor lines. `MajorStep` must be greater than or equal to `MinorStep`.

### Isometric grid

The isometric grid uses three line families:

```text
vertical lines
diagonal lines at +IsometricAngleDegrees
diagonal lines at -IsometricAngleDegrees
```

The default angle is `30°`. The vertical line spacing is derived from the diagonal spacing so that vertical lines pass through the vertices created by the intersections of the two diagonal families:

```text
verticalStep = diagonalSpacing / (2 * tan(angle))
```

For the default 30-degree grid, this creates the expected isometric vertex alignment instead of a visually independent set of vertical lines.

### Zoom-based visibility

When the user zooms far out, grid lines become too dense to be readable or useful. When the user zooms too far in, a given grid level may also become visually unhelpful. `GridSettings` therefore stores a screen spacing range.

Rule:

```text
if the screen distance between adjacent grid lines is below MinimumScreenSpacing
-> that grid level is suppressed

if the screen distance between adjacent grid lines is above MaximumScreenSpacing
-> that grid level is suppressed
```

This keeps the canvas readable across zoom levels.

### GridSettings model

Current `GridSettings` properties:

```text
MinorStep                 minor grid spacing in model units
MajorStep                 major grid spacing in model units
Step                      compatibility alias for MinorStep
OriginX                   grid origin X
OriginY                   grid origin Y
IsVisible                 visual grid toggle
Kind                      Rectangular or Isometric
IsometricAngleDegrees     angle used by isometric diagonal families
MinimumScreenSpacing      lower screen spacing threshold
MaximumScreenSpacing      upper screen spacing threshold
```

The helper `GetIsometricVerticalStep(diagonalSpacing)` calculates the required vertical-line spacing for the current isometric angle.

### Grid snapping alignment

Grid snapping uses `GridSettings` and `SnapKind.Grid`.

For rectangular grids, the closest grid candidate is the nearest rectangular grid point.

For isometric grids, the closest candidate is an isometric vertex generated from the same layout used by the renderer. This keeps the visible grid and grid snap behavior aligned.

---

## Drawing Configuration

Drawing configuration is the planned document-level area for parameters that govern measurement units, display precision, dimension appearance and default tool behavior.

This is distinct from application settings, which are user-local and session-specific.

### Planned DrawingSettings

`DrawingSettings` is planned as document data inside `CadDocument`, serialized as part of the `.opencad2d.json` document format.

It should contain:

```text
Units               measurement unit system (mm, cm, m, inch, feet)
LinearPrecision     decimal places for length display and dimension values
AngularPrecision    decimal places for angle display
DefaultDimensionStyleId   id of the style used for new dimensions
GridSettings        minor/major step, origin, visibility, kind, isometric angle, screen thresholds
DefaultSnapTolerance   model-unit tolerance for snapping
DefaultTextHeight   default height for new text entities
```

When this is implemented, opening a document should load `DrawingSettings` and apply them to tools, the status bar and dimension entities.

### Current grid settings behavior

At the current implementation stage, grid settings are applied to the active `CadWorkspace` through `SetGridSettings(...)`. They affect rendering and grid snap behavior immediately.

A later document-level `DrawingSettings` command can make these settings undoable and persist them as part of the drawing file. Until that exists, keep grid-setting behavior concentrated in the workspace/app layer and avoid duplicating it inside individual tools.

### Configuration UI

The current configuration UI is `GridSettingsWindow`, hosted by `OpenCad2D.App`. It reads the current `GridSettings`, validates user input in `GridSettingsWindowViewModel`, and returns a `GridSettingsResult` only when the user confirms with `OK`.

### Separation from session settings

Document settings and session settings serve different purposes and are stored separately:

```text
DrawingSettings (planned document settings)
  -> saved in .opencad2d.json
  -> shared with anyone who opens the file
  -> changing them should mark the document dirty

Session settings (application)
  -> saved in settings.json
  -> user-local
  -> do not affect the drawing content
```

When document-level settings are implemented, a document opened on a different machine should use the same `DrawingSettings`. Each machine still uses its own session settings, such as window position and shortcuts.
