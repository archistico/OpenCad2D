# Dynamic HUD keyboard contract smoke checklist

Reference: `docs/specs/v0.8.121-dynamic-command-hud.md`, keyboard and validation contract.

Automated window coverage lives in `CommandHudKeyboardTests` and uses Avalonia Headless 12.0.2, matching the application's Avalonia version. Run `dotnet test tests/OpenCad2D.App.Tests --filter FullyQualifiedName~CommandHudKeyboardTests`.

The following desktop checks supplement automation for native keyboard layout, clipboard and visual positioning. They are a manual checklist, not a record of a completed desktop run.

| Workflow | Expected result |
|---|---|
| LINE, pick start, type `25`, Tab, `90`, Enter | Segment length 25, angle 90 degrees; Undo/Redo restores the operation. |
| LINE, pick start, Tab, type `12.3456789`, move mouse, Tab, Enter | Focus, draft and caret survive mouse motion; stored length keeps its precision and initial direction. |
| Tab/Shift+Tab through a point prompt and a Distance/Angle/X/Y prompt | Only editable fields participate; navigation wraps in both directions. |
| Enter `-5` as a line distance; press Tab, Enter and right click | Each rejects the input without drawing, clearing the draft or submitting an older distance; the error appears in the HUD. Correct to `30` and confirm. |
| Type line distance `25`, then left click far from the start; repeat with `-5` | Valid input creates a segment of exactly 25; invalid input creates no geometry and restores editing focus to the retained draft. |
| Clear one edited field using Backspace/Delete; navigate away | Only that field's override is cleared; other measurements remain. Enter on a deliberately empty edited field reports an error. |
| POINT, enter only X, Tab, Enter | No point is created. Enter Y and confirm to create the exact UCS point. |
| RECTANGLE, choose first corner, enter Width, Tab, Height, Enter | Navigation works even before a nonzero height is previewed; exact dimensions are committed. |
| RECTANGLE, first corner away from drawing origin, move toward each of the four quadrants, type Width, Tab, Height, Enter; repeat after zoom/pan | Typed sizes preserve the mouse preview direction through typing and field navigation. Preview and final rectangle agree; Undo/Redo preserves the result. |
| RECTANGLE, type dimensions immediately after the first click; repeat while moving the mouse to another quadrant between typing and Enter | With no indicated direction, sizes use positive UCS axes. Real mouse motion can change quadrant without changing typed sizes; marker cleanup never moves the drawing cursor to the origin. |
| POLYGON, type `12`, Tab, move mouse, Enter | Twelve sides remain staged; the tool advances to center selection. `257`, fractions and overflow values are rejected. |
| DIVIDE with a selected line, type `12`, Tab, move mouse, Enter | Eleven division points; the original line remains unchanged. |
| POLYLINE with a start point, type distance, Tab, press A/L/U/C where offered | The requested option runs; previous numeric overrides do not leak into the new mode. |
| FILLET, R, Tab, Enter without typing | The prompt's default radius is retained. Entering zero is accepted. |
| DOOR/WINDOW, Tab to a coordinate, choose W/T/O, enter value, Tab, move mouse, Enter | The option remains accessible; the Value field persists until confirmation. Invalid domain values remain editable. |
| Type or paste `100,50`, `@10,5`, `10<45`, `@10<45` from canvas | Whole expressions reach the command parser. A dot denotes a direct decimal distance in point prompts. |
| Tab into a scalar field and enter `12,5` with Italian keyboard/numpad | Decimal comma is accepted as a scalar, without becoming a coordinate pair. |
| Edit a property or dialog text box while a command is active | Typing, Enter and arrow keys stay in that editor and do not submit CAD input. |
| Esc while editing; then start another command | The command is cancelled and its values/error are cleared. Hovering over the HUD never takes keyboard focus or blocks canvas picking. |
| TEXT/MTEXT, enter X and Y, Tab, Enter | The asynchronous text dialog opens once for the specified insertion point. |

Before handoff, run `dotnet build OpenCad2D.sln` and `dotnet test OpenCad2D.sln --no-build` in addition to focused checks.
