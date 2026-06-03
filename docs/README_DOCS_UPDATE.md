# OpenCad2D documentation update notes

This file explains how documentation updates should be applied to the repository.

The documentation lives under `docs/` and is written in Markdown. The User Guide and the project documentation are written in English. For now, GitHub is the only publication target, so every page should remain readable directly in the GitHub Markdown viewer.

Documentation updates should stay simple. Update packages should contain only Markdown files, images, GIFs, examples, and other documentation assets that are meant to remain in the repository. Do not add Python helper scripts, local tooling folders, generated documentation sites, or project-specific documentation utilities.

The preferred writing style is direct, practical, and readable. Use normal paragraphs when they explain a workflow better. Use bullet lists for indexes, quick references, command summaries, checklists, and step-by-step procedures. The goal is not to remove lists, but to avoid making every page feel like a fragmented checklist.

Images and GIFs should be stored under `docs/assets/`. File names should be lowercase and descriptive, for example `zoom-window.gif`, `dynamic-hud-distance-angle.gif`, or `layer-manager.png`. Avoid date-based screenshot names unless the image is part of a specific test report.

When a feature changes, update the matching User Guide chapter in the same development step. If the change affects command aliases, shortcuts, export behavior, snapping, layers, the Library, external images, or the HUD, also check the relevant reference pages and troubleshooting page.
