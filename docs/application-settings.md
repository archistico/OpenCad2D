# Application and Document Settings

OpenCad2D separates portable document settings from local user/session settings.

---

## Document-level settings

Document-level drafting settings are stored inside `.opencad2d.json` because they affect how a drawing should reopen and be edited.

Stored in the native document:

- grid visibility/type/spacing/origin where supported;
- snap enabled state;
- active snap modes;
- snap tolerance;
- Ortho mode;
- Polar Tracking mode;
- current layer;
- current text format;
- current dimension style where available;
- viewport state.

Old files without the `settings` section must load with safe defaults.

---

## Local session settings

Local session settings remain outside `.opencad2d.json`.

Implemented in v0.9 Phase 1:

- last opened native drawing file path;
- last open directory;
- last save directory;
- last export directory;
- recent native drawing files, capped to 10 entries.

The app must tolerate missing, partial, empty, unreadable or corrupt local settings by falling back to safe defaults. Local settings persistence must not block opening, saving or exporting drawings.

Deferred for later:

- theme;
- window size/position;
- panel widths;
- shortcut preferences.

Default location:

```text
Windows   %APPDATA%\OpenCad2D\settings.json
Linux     ~/.config/OpenCad2D/settings.json
macOS     ~/Library/Application Support/OpenCad2D/settings.json
```

---

## Rule

```text
.opencad2d.json       portable drawing state and drafting settings
settings.json         local user/session preferences
```

Do not put drawing content in local settings. Do not put personal UI preferences in portable drawing files unless they are part of the drawing workflow.
