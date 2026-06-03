# Export

Export creates external files from the current drawing. These files are useful for sharing, printing, publication, or exchange with other software. Export is different from Save: saving preserves the editable OpenCad2D project, while exporting creates a derived output.

The native project file remains `.opencad2d.json`. Even after exporting to SVG, DXF, PDF, or PNG, keep the native file if you want to continue editing the drawing later.

## SVG

SVG is a vector format suitable for web pages, documentation, illustrations, and workflows where scalable graphics are needed. It preserves vector shapes and can be useful for showing drawings in browsers or embedding them in project documentation.

SVG export should reproduce the visible drawing appearance as closely as possible, including line formats, text, dimensions, fills, and image references according to the current exporter support.

## DXF

DXF is an interchange format used to exchange CAD geometry with other CAD applications. It is useful when OpenCad2D drawings need to be opened or processed by external CAD software.

Because DXF is an interchange format, not every native OpenCad2D concept may map perfectly to it. When using DXF, verify layers, line formats, dimensions, blocks, and text in the receiving application.

## PDF

PDF is useful for printing, sharing final drawings, and sending documents to people who do not need to edit the CAD file. It should be treated as a final or review-oriented output, not as the editable source of the project.

When exporting to PDF, check scale, page size, margins, line weight, text readability, and whether external images are displayed correctly.

## PNG

PNG is a raster image format. It is useful for previews, quick sharing, documentation screenshots, and cases where a bitmap image is more convenient than a vector file.

Unlike SVG or DXF, PNG does not preserve editable vector geometry. Once exported to PNG, lines, dimensions, and text become pixels.

## Export does not save the project

Export should not update the current native file path, should not clear the dirty state of the drawing, and should not be treated as a project save. A safe workflow is to save the `.opencad2d.json` project first and then export the needed external format.

This distinction prevents accidental data loss. The exported file may be correct for sharing, but the native file is the one that preserves the full editable state.

## Recommended screenshots

The export chapter should show the export buttons or menu, one simple drawing exported to SVG or PDF, and a short explanation of which format to choose. The first version of the documentation can use static screenshots; later, the release documentation can include example exported files generated from drawings in `docs/examples/`.

## Visual assets to add

The export chapter should show where export commands are located and what the result looks like. Recommended assets are `docs/assets/images/export/export-toolbar.png`, `docs/assets/images/export/export-format-choice.png`, `docs/assets/images/export/exported-pdf-example.png`, and `docs/assets/images/export/exported-svg-example.png`.
