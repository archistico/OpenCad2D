# File Management

This chapter explains how OpenCad2D handles native drawings and file operations. The most important distinction is between saving the editable project and exporting a derived file for sharing, plotting, or interchange.

## New

New creates an empty drawing. It is used when you want to start a new project from scratch. If the current drawing contains unsaved changes, OpenCad2D should protect the user from losing work by asking for confirmation before replacing the current document.

A new drawing should start with the default document settings, default layers, default line formats, text formats, and dimension styles provided by the application.

## Open

Open loads an existing native OpenCad2D drawing and replaces the current document with it. The native drawing format is `.opencad2d.json`.

Use Open when you want to continue editing a drawing that was previously saved by OpenCad2D. Opening a drawing is different from importing one: Open replaces the current document, while Import Drawing brings another drawing into the current one.

## Save

Save writes the current drawing to its existing file path. After a successful save, the current native project is up to date and the drawing should no longer be marked as dirty.

Use Save during normal work to protect progress. Save preserves the editable OpenCad2D model, including entities, layers, reusable formats, dimensions, blocks, image references, and other native information supported by the project format.

## Save As

Save As writes the current drawing to a new file path. It is used when the drawing has never been saved before, when you want to create a copy, or when you want to preserve an earlier version under a different name.

After Save As, the new path becomes the current file path for future Save operations.

## Native drawing format

OpenCad2D stores editable projects as JSON files using the `.opencad2d.json` extension. This is the source format of the application and should be considered the authoritative project file.

Exported files such as SVG, DXF, PDF, or PNG are derived outputs. They are useful, but they are not a replacement for the native drawing file if you want to continue editing the project later.

## Import Drawing

Import Drawing inserts the contents of another `.opencad2d.json` file into the current drawing. This is useful when combining plans, reusing a detail, inserting a prepared drawing, or bringing library-like content into a project.

During import, OpenCad2D should merge compatible resources where possible. If a layer or reusable format already exists with the same meaning, the import should avoid unnecessary duplicates. If two resources have the same name but different definitions, the behavior must remain predictable and should be documented in the import-specific technical reference.

## External image references

Drawings may contain image references, for example attached raster images used as backgrounds, scans, or tracing references. These images are external files. The drawing stores the reference, but the raster file itself must remain available on disk unless references are collected beside the drawing.

Use the image tools when an image has moved, must be replaced, has the wrong aspect ratio, or needs to be collected with the drawing before sharing the project.

## Save and Export are different

Save and Save As update the native editable project. Export creates an external representation, such as SVG, DXF, PDF, or PNG. Export should not silently replace the native drawing file and should not be treated as saving the project.

A safe workflow is to save the `.opencad2d.json` file first, then export the format needed for printing, sharing, publication, or exchange with other software.
