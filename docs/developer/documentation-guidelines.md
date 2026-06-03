# Documentation Guidelines

OpenCad2D documentation is written in Markdown and stored in the repository. For now, GitHub is the publication target. Every page should therefore be readable directly from the GitHub web interface without requiring a separate documentation generator.

The User Guide and project documentation are written in English. The User Guide explains the application from the user's point of view and avoids internal implementation details unless they affect the visible workflow. Technical notes, implementation constraints, test plans, release checklists, and architecture decisions belong in developer or reference documents, not in the user-facing guide.

Keep the documentation lightweight. The repository should not include documentation tooling folders or Python scripts just to maintain the guide. Documentation updates should be made directly in Markdown, with images, GIFs, and examples stored under the existing `docs` structure. Any validation or cleanup command that is only useful once should be described in the update note rather than committed as a script.

Use a direct, practical, readable style. Prefer short paragraphs when they explain a behavior naturally. Use bullet lists when they make the page easier to scan, especially in indexes, quick references, command summaries, checklists, and step-by-step procedures. The goal is not to remove lists, but to avoid making every page feel like a fragmented checklist.

A good user-facing page normally explains what the tool does, when to use it, how the interaction flows, which precision aids are relevant, and what result the user should expect. The tone should be functional and concrete, not promotional.

Screenshots and GIFs should be stored under `docs/assets/`. Use lowercase file names with hyphens. A good asset name describes the feature, not the date of the screenshot. For example, use `zoom-window.gif` rather than `screenshot-2026-06-03.gif`. More detailed capture rules are tracked in [Image and GIF Capture Guidelines](image-capture-guidelines.md).

When a feature changes, update the documentation in the same development step. A command change usually affects one User Guide chapter, the command/alias reference, and sometimes the troubleshooting page. A release-related change may also affect the release checklist or AI handoff.

The documentation should not claim that a feature exists before it is implemented. When describing planned behavior, mark it clearly as future work or keep it in planning documents rather than in the main User Guide.
