# OpenCad2D Agent Guide

## Scope

These instructions apply to the entire repository.

OpenCad2D is a C#/.NET 8 desktop CAD application built with Avalonia UI. Prefer small, testable changes that preserve the existing separation between geometry, document logic, interaction, tools, persistence, export, and UI.

Before changing a feature, read the relevant technical document under `docs/`. Use `docs/architecture.md` for dependency and ownership rules, `docs/roadmap.md` for current priorities, and `docs/ai-handoff.md` for the latest implementation handoff. Detailed feature contracts live under `docs/specs/`.

## Build and Test

The pinned SDK is .NET `8.0.420`.

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
dotnet run --project src/OpenCad2D.App
```

`make check` is the repository shortcut for build plus test.

During development, run the narrowest relevant test project or filtered test first. Before handing off a code change, run the full solution build and test suite unless the environment prevents it. Report any checks that were not run.

## Project Boundaries

Keep dependencies flowing in this direction:

```text
App -> Persistence -> Core -> Geometry
App -> Export -> Core -> Geometry
App -> Tools -> Interaction -> Core -> Geometry
```

- `OpenCad2D.Geometry`: pure geometry, transformations, intersections, distances, and tolerances. Do not add CAD document or UI concepts.
- `OpenCad2D.Core`: entities, layers, styles, documents, commands, command history, and domain services.
- `OpenCad2D.Interaction`: UI-independent hit testing, selection, snapping, and grid behavior.
- `OpenCad2D.Tools`: UI-independent drawing/editing tools, input constraints, previews, and workspace behavior. Do not depend on Avalonia.
- `OpenCad2D.Persistence`: native document serialization. Do not depend on App, Tools, or Interaction.
- `OpenCad2D.Export`: derived SVG, DXF, and PDF output. Exporters are read-only with respect to the document and dirty state.
- `OpenCad2D.App`: Avalonia views, rendering, dialogs, input forwarding, and ViewModels.

Do not bypass these boundaries to avoid adding a small abstraction in the correct layer.

## Domain Rules

- Treat `CadDocument` as the public mutation boundary. Use its `AddEntity`, `RemoveEntity`, `RemoveEntities`, `ReplaceEntity`, and `ReplaceEntities` methods instead of mutating `EntityCollection` directly.
- Route user-visible document changes through undoable commands. Keep dirty-state behavior based on command-history generations.
- Preserve native entity geometry in CAD editing operations. Sampling may support calculations or previews, but it must not replace an available native representation.
- Reuse shared curve splitting, intersection, snapping, and input-constraint services instead of implementing entity-specific copies in tools or UI code.
- Store entities in WCS/model coordinates. Convert typed UCS input before it reaches the active tool.
- Keep tool previews UI-independent through the existing preview provider protocols. Rendering details belong in App.
- Resolve stroke appearance through `Entity -> Layer -> LineFormat`; resolve solid fill through the layer. Do not introduce per-entity style overrides without an explicit architecture change.
- Keep native persistence separate from export. Export must not mutate the document, change `CurrentFilePath`, call `MarkSaved()`, or clear the dirty marker.
- Preserve locked-layer, visibility, selection, and snapping semantics. Rendering filters are not mutation rules.

## Coding Conventions

- Follow `.editorconfig`: UTF-8, LF endings, four-space indentation, final newline, and no trailing whitespace except where Markdown requires it.
- Nullable reference types and implicit usings are enabled.
- Match the surrounding C# style: file-scoped namespaces, explicit access modifiers, descriptive names, guard clauses, and immutable domain values where practical.
- Keep UI code thin. Put reusable behavior in the lowest valid UI-independent project.
- Avoid broad refactors in feature or bug-fix changes unless they are necessary for correctness.
- Do not edit generated build output under `bin/`, `obj/`, `artifacts/`, or test result directories.

## Tests

Tests use xUnit and mirror the source projects under `tests/`.

- Add or update tests in the corresponding test project for every behavior change.
- Name tests in the established `MethodOrScenario_Condition_ExpectedResult` style.
- Cover regressions at the lowest layer that owns the behavior.
- For document mutations, test undo/redo, locked-layer rules, and dirty-state implications when relevant.
- For geometry and editing, include tolerance and degenerate/edge cases and verify native precision.
- For persistence changes, add round-trip and backward-compatibility coverage where applicable.
- For exporters, verify output and confirm that document state remains unchanged.
- UI workflows that cannot be adequately automated should include or update a focused checklist under `docs/testing/`.

## Documentation and Change Discipline

- Keep code, tests, and relevant documentation aligned.
- Update `docs/architecture.md` when changing ownership or dependency rules.
- Update the relevant specification or user guide when behavior visible to users changes.
- Treat `docs/roadmap.md` as the active roadmap; do not infer current priorities from old milestone notes.
- Preserve existing public file-format compatibility unless a documented migration is part of the task.
- Do not overwrite unrelated working-tree changes. Keep patches scoped to the requested work.
