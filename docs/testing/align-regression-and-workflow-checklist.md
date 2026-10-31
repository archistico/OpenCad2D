# ALIGN regression and workflow checklist

Automated coverage: `AlignToolTests`, `AlignWorkflowRegressionTests`, `AlignTransformServiceTests` and the ALIGN cases in `CommandHudKeyboardTests` / `MainWindowViewModelCommandLineTests`.

These checks supplement headless automation; they do not record a completed desktop test.

- Start ALIGN without preselection. The HUD requests objects and has no X/Y fields. Click an object, Shift-click to add/remove, Ctrl-click to cycle overlaps, and drag inside/crossing windows. Selection stays active until Enter/right-click. Empty selection must not advance.
- Preselect editable objects and start ALIGN: the first source point is requested immediately. Hidden/locked-layer objects are excluded. A visible locked destination object may still supply geometric reference snaps.
- Pick source 1, destination 1, source 2, destination 2. Verify the numbered prompts, X/Y fields, reference markers and guide lines. The first destination previews translation; the second previews rotation without scaling. No dirty marker or history entry appears until confirmation.
- Repeat with native circles, arcs, elliptical curves and mixed polylines, and with several objects. Geometry types, bulges and ids are preserved. Undo restores all sources in one operation; Redo restores the result and dirty state.
- With unequal reference lengths, verify Y scales to match both pairs; N, Enter and right click preserve size. Completion returns to Selection. Ctrl+Y must not submit Yes.
- Enter relative coordinates for source 2: the offset is from source 1. Destination 2 offsets are from destination 1. Repeat with a translated/rotated UCS and exact HUD X/Y entries.
- Hover a valid second destination, then hover/click the first destination or a point within distance tolerance. The old rotation preview clears, no mutation occurs and HUD feedback explains recovery. Choose a distinct point to resume.
- At each reference step and the scale prompt use Back/B. Only the relevant point and dependent preview are cleared. Objects/O returns to object selection without modifying the document. Esc cancels the preview and any active selection window.
- Remove, replace, hide or lock a selected source, or change the selection, before confirmation. Old points/preview must clear and the command must request selection review. An unrelated newly selected entity must never receive the old transform.
