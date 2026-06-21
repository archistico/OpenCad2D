# OpenCad2D first Library content pack

This folder contains the first curated static Library pack for OpenCad2D.

The files are ordinary `.opencad2d.json` drawings and are intentionally simple: they avoid nested block references and use only first-pass entities that the Library Browser can preview and insert as block definitions. The application project already copies `library/**` to build and publish output through `src/OpenCad2D.App/OpenCad2D.App.csproj`.

Insertion rule for this first pack:

- furniture, sanitary fixtures and kitchen fixtures use the object center as `(0,0)`;
- symbols use the natural symbol reference point as `(0,0)`;
- the graphic scale starts at its left zero mark.

Doors, windows and stairs are deliberately not included here as static files. Stairs already have a parametric tool, while doors and windows are planned as parametric entities with HUD anchor selection and wall masking/opening behavior.
