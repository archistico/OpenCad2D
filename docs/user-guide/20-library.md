# Library

The Library is used to insert reusable static drawing objects stored as native `.opencad2d.json` files. It is intended for furniture, fixtures, symbols, people, vehicles, outdoor elements, annotation marks, and other reusable 2D objects.

The Library should reduce the number of dedicated toolbar buttons. Instead of adding a separate command for every fixed object, OpenCad2D can load many reusable drawings from the Library folder and show them in a browser with categories and previews.

## Library versus Symbols

OpenCad2D keeps a clear distinction between static library objects and parametric symbols. Static objects belong in the Library. Parametric objects remain tools because they need dimensions or options from the user.

The current design decision is that doors, windows, and stairs are parametric objects. They should be handled by dedicated tools. Other reusable objects, such as furniture, sanitary fixtures, electrical symbols, vehicles, people, trees, and annotation graphics, should normally be static Library items.

## Folder structure

The Library folder is scanned recursively for `.opencad2d.json` files. The first folder below the Library root can be used as the visible category. Subfolders can be used to keep the files organized.

A practical structure is to keep major groups such as Furniture, Bathroom, Kitchen, Living Room, Bedroom, Office, Electrical, Plumbing, People, Vehicles, Outdoor, Sections, Elevations, and Annotation Symbols. Each object variant should be a separate file, especially when top, front, and side views are needed.

For example, a table may have separate files for top view, front view, and side view. This makes the object easy to insert in plans, elevations, and sections without adding more application commands.


## Included objects

The current repository includes a first small Library pack. It contains top-view furniture, kitchen fixtures, sanitary fixtures and two symbols. This pack is intentionally limited so that the insertion and publish workflow can be tested before the Library grows.

The first categories are `arredo`, `cucina`, `sanitari` and `simboli`. Furniture, kitchen and sanitary objects insert from their center point. The north symbol and graphic scale use their natural reference point.

## Creating a library item

A Library item is just an OpenCad2D drawing prepared for reuse. Draw the object with normal CAD entities, place the intended insertion point at model origin `(0,0)`, then save the file as `.opencad2d.json` inside the appropriate Library category.

The origin is important. When the object is inserted, `(0,0)` in the library file becomes the point picked by the user in the current drawing. If the origin is poorly placed, insertion will feel imprecise.

## Inserting a library item

The Library browser should let the user choose a category, inspect a preview, and insert the selected item into the current drawing. The insertion point is picked on the canvas and can use the normal snap system.

Inserted objects are expected to behave like reusable drawing content. They can be selected, moved, copied, rotated, scaled, and exploded when raw editable geometry is needed.

## Preview images

The Library should show a vector preview generated from the actual `.opencad2d.json` file, not a separate manually maintained thumbnail when possible. This keeps the preview consistent with the inserted content.

Documentation screenshots should show the Library browser with a small but realistic set of items. The first example should avoid too many categories. It is better to show one clean workflow: open Library, select a category, preview an item, insert it, then move or rotate it.

## Publishing with the application

The Library folder is part of the usable application package. When preparing a release or running publish, the Library folder must be copied beside the executable so the browser can find the available objects.

If the Library is missing after publishing, the user may see an empty browser even though the application is working correctly. This should be covered in the troubleshooting chapter as well.

## Visual assets to add

The Library workflow should be shown as one complete path: open the browser, select a category, preview an object, insert it, and edit the inserted content if needed. Recommended assets are `docs/assets/images/library/library-browser-overview.png`, `docs/assets/gifs/library/insert-library-item.gif`, and `docs/assets/images/library/library-folder-example.png`.
