# Known limitations

OpenCad2D is an experimental open-source 2D CAD application. This document lists the main limitations that should be visible to users and contributors before the first stable release.

## Native save vs export

The native editable project format is `.opencad2d.json`.

Export commands create external files such as SVG, DXF or PDF, but they do not save the native drawing, do not change `CurrentFilePath` and do not clear dirty state.

Use **Save** or **Save As** to save the editable OpenCad2D project. Use export only to create interchange, vector or print/share files.

## DXF compatibility

DXF import and export currently target a focused 2D subset. Automated tests verify the internal DXF structure, representative entity records, layer records, linetypes, lineweights, text records and graphical dimension export.

External compatibility validation in LibreCAD, QCAD and Autodesk DWG TrueView is still required before v1.0. Until that validation is complete, DXF support should be treated as experimental interoperability support.

## Dimensions

Dimensions are currently non-associative. They store their own measured points and annotation geometry.

If the original measured entity is moved, edited or deleted, the dimension does not update automatically. The user must update or recreate the dimension manually.

Associative dimensions are planned as a post-v1.0 advanced feature.

## Polar Tracking and Rotate

Polar Tracking currently applies to point-placement workflows such as drawing lines and polylines, moving entities and similar cursor-driven placement operations.

`RotateTool` computes its angle from base/reference/destination points. Ortho can constrain interactive rotation to 90-degree directions, but Rotate does not yet use the selected Polar Tracking angle step for explicit angle computation.

## Trim and Extend

Break, Trim and Extend support has been expanded for lines, arcs, circles and polylines where the current geometry services define a reliable operation.

Two-cutting-edge Trim is currently implemented for line targets. Broader multi-boundary Trim behavior for arcs, circles and polylines is planned for future geometry refinement. Unsupported operations should report a clear status message instead of silently doing nothing.

## Property Panel and polylines

The Property Panel v2 supports undoable edits for many primary entity properties.

Detailed numeric editing of individual polyline vertices is not yet available as a Property Panel vertex table. Polyline vertices are currently edited through grip editing.

A future Property Panel improvement should add a vertex table with index, X, Y, add/remove vertex operations and open/closed state management.

## Application settings

Several drawing aids and UI preferences are still runtime/session settings. Full persistence for application settings, shortcuts, last file and default grid preferences is planned before v1.0.

## Draw order / Z-order

Draw order is not yet independent from layers/entities. A dedicated draw order / Z-order system is planned before v1.0 so users can explicitly send entities forward/backward without abusing layer order.
