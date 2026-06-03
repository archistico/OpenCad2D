# Introduction

OpenCad2D is a free and open-source 2D CAD application designed for precise technical drawing. Its scope is intentionally clear: it is a 2D CAD, not a 3D modeling program. The application focuses on the workflows that matter in a 2D drawing environment: creating geometry, editing it accurately, organizing it with layers and styles, adding dimensions and annotations, using references, inserting reusable objects, and exporting the result to common formats.

The project is built around a practical balance between mouse-driven drawing and command-style precision. You can work visually on the canvas, but you can also type exact distances, angles, coordinates, radii, and other command values through the Dynamic HUD. This makes OpenCad2D suitable for quick drafting as well as measured technical work.

## What OpenCad2D is for

OpenCad2D is intended for drawings where 2D precision, clarity, and editability are more important than visual effects. Typical use cases include architectural plans, furniture layouts, technical diagrams, measured sketches, reference tracing, symbol-based drawings, and drawings that must be exported to SVG, DXF, PDF, or PNG.

The native project format is `.opencad2d.json`. This is the editable file that should be kept as the source of the drawing. Exported files are useful for sharing, printing, publishing, or exchanging data with other software, but they do not replace the native project file.

## Main concepts

A drawing is made of entities such as lines, polylines, arcs, circles, ellipses, text, dimensions, images, blocks, and library objects. These entities can be organized on layers, styled through line formats and text formats, measured, edited, selected, and exported.

Precision is handled through several complementary systems. Snaps help you pick meaningful points on existing geometry. Ortho constrains movement to main directions. Polar Tracking helps follow predefined angles. Grid and Grid Snap help with regular spacing. The Dynamic HUD lets you enter exact numeric values without leaving the canvas.

## How to read this guide

Start with the interface, canvas navigation, and file management chapters if this is your first time using the application. Then read the Dynamic HUD and Snaps chapters, because they explain the behavior that appears across almost every drawing and editing command.

After that, move through the tool chapters according to what you need: Draw Tools for creating geometry, Select Tools for choosing objects, Edit Tools for modifying existing entities, Dimensions and Measure Tools for annotation and checking, then Images, Layers, Library, and Export.

## Documentation status

This guide is maintained with the repository and evolves with the application. Some chapters may describe behavior that is being refined during development, especially where the user interface, Library workflow, or release packaging is still changing. When a feature changes, the matching documentation page should be updated together with the code or immediately after the change is verified.

Recommended visual asset for this chapter: `docs/assets/images/interface/main-window-overview.png`.
