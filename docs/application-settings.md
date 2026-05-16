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

Local session settings should remain outside `.opencad2d.json`.

Examples:

- theme;
- window size/position if implemented;
- recent files;
- last opened directory;
- panel widths.

Suggested future location:

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
