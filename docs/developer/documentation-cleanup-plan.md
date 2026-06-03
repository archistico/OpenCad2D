# Documentation Cleanup Plan

This file tracks the cleanup decisions for the `docs` folder.

The User Guide and developer documentation should be in English. Documentation is currently published through GitHub Markdown files only.

The documentation folder should contain Markdown files, documentation assets, examples, specs, testing notes, license notices, and release notes. It should not contain Python helper scripts, generated documentation sites, local tooling folders, or one-off maintenance utilities.

If the previous documentation update was applied, remove this file if it exists:

```bat
del docs\check-doc-links.py
```

Also remove these folders if they were created locally during documentation experiments and are not used for actual documentation assets:

```bat
rmdir /s /q docs\tools
rmdir /s /q docs\tool
rmdir /s /q docs\scripts
```

Do not delete `docs/tools.md`: that is a technical documentation page, not a tooling folder.

The old Italian user-guide files should already be gone. If they are still present, remove them:

```bat
del docs\user-guide\00-introduzione.md
del docs\user-guide\01-interfaccia.md
del docs\user-guide\02-navigazione-canvas.md
del docs\user-guide\03-gestione-file.md
del docs\obsolete-documents.md
```

Keep the existing root-level technical documentation for now. Do not move files yet unless all links are updated.

The next phases are to refine the User Guide chapter by chapter, add screenshots and GIFs under `docs/assets/`, verify the shortcuts and command aliases table, and later reorganize technical documents into clearer reference, development, and release sections.
