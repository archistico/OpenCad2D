# Branding and logo assets

OpenCad2D keeps its current logo sources in the repository `screenshot` folder.

![OpenCad2D logo](../screenshot/logo_opencad2d.svg)

## Source assets

```text
screenshot/logo_opencad2d.af
screenshot/logo_opencad2d.svg
screenshot/logo_opencad2d_16.png
screenshot/logo_opencad2d_32.png
screenshot/logo_opencad2d_64.png
screenshot/logo_opencad2d_128.png
screenshot/logo_opencad2d_256.png
screenshot/logo_opencad2d_512.png
```

## Application assets

The Avalonia application uses copied runtime assets under:

```text
src/OpenCad2D.App/Assets/app-icon.ico
src/OpenCad2D.App/Assets/logo_opencad2d_128.png
src/OpenCad2D.App/Assets/logo_opencad2d_256.png
```

`app-icon.ico` is generated from the PNG logo resolutions and is configured in `OpenCad2D.App.csproj` through `ApplicationIcon`.

All Avalonia windows set:

```xml
Icon="/Assets/app-icon.ico"
```

The About window uses the 128 px logo image instead of the previous temporary `OC` placeholder.

## Documentation usage

Use the SVG logo in Markdown documentation when possible:

```markdown
![OpenCad2D logo](../screenshot/logo_opencad2d.svg)
```

For the root `README.md`, use:

```markdown
![OpenCad2D logo](screenshot/logo_opencad2d.svg)
```

Keep the screenshot files in the `screenshot` folder so documentation paths remain stable.
