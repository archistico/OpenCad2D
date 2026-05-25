using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.ImageReferences;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;
using OpenCad2D.Tools.Common;
using System;
using System.IO;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelImageReferenceTests
{
    [Fact]
    public void ReplaceSelectedImageReference_ShouldRelinkSelectedImageAndKeepGeometry()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.AddImageReference(
            "old.png",
            Point2D.Origin,
            width: 10,
            height: 5,
            pixelWidth: 100,
            pixelHeight: 50);

        Assert.True(viewModel.HasSingleSelectedImageReference);

        viewModel.ReplaceSelectedImageReference(
            "new.jpg",
            pixelWidth: 400,
            pixelHeight: 200);

        ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        Assert.Equal("new.jpg", image.FilePath);
        Assert.Equal(400, image.PixelWidth);
        Assert.Equal(200, image.PixelHeight);
        Assert.Equal(10, image.Width, precision: 6);
        Assert.Equal(5, image.Height, precision: 6);
        Assert.True(viewModel.HasSingleSelectedImageReference);
    }

    [Fact]
    public void ResetSelectedImageReferenceAspectRatio_ShouldUsePixelAspectAndKeepCenter()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.AddImageReference(
            "photo.jpg",
            new Point2D(10, 5),
            width: 12,
            height: 20,
            pixelWidth: 1200,
            pixelHeight: 800);

        viewModel.ResetSelectedImageReferenceAspectRatio();

        ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        Assert.Equal(new Point2D(10, 5), image.Center);
        Assert.Equal(12, image.Width, precision: 6);
        Assert.Equal(8, image.Height, precision: 6);
        Assert.True(viewModel.HasSingleSelectedImageReference);
    }

    [Fact]
    public void ResetSelectedImageReferenceAspectRatio_WithoutImageSelection_ShouldNotChangeDocument()
    {
        var viewModel = new MainWindowViewModel();

        ToolResult result = viewModel.ResetSelectedImageReferenceAspectRatio();

        Assert.False(result.Changed);
        Assert.Empty(viewModel.Workspace.Document.Entities.All);
    }

    [Fact]
    public void MissingImageReferenceCount_ShouldCountOnlyMissingImageFiles()
    {
        string existingFile = Path.GetTempFileName();

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.AddImageReference(
                existingFile,
                Point2D.Origin,
                width: 10,
                height: 5,
                pixelWidth: 100,
                pixelHeight: 50);

            viewModel.AddImageReference(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png"),
                new Point2D(20, 20),
                width: 10,
                height: 5,
                pixelWidth: 100,
                pixelHeight: 50);

            Assert.True(viewModel.HasMissingImageReferences);
            Assert.Equal(1, viewModel.MissingImageReferenceCount);
        }
        finally
        {
            File.Delete(existingFile);
        }
    }

    [Fact]
    public void SelectNextMissingImageReference_ShouldSelectFirstMissingReference()
    {
        string existingFile = Path.GetTempFileName();
        string missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.AddImageReference(
                existingFile,
                Point2D.Origin,
                width: 10,
                height: 5,
                pixelWidth: 100,
                pixelHeight: 50);

            viewModel.AddImageReference(
                missingFile,
                new Point2D(20, 20),
                width: 8,
                height: 4,
                pixelWidth: 80,
                pixelHeight: 40);

            ToolResult result = viewModel.SelectNextMissingImageReference();

            Assert.True(result.Changed);
            ImageReferenceEntity selected = Assert.IsType<ImageReferenceEntity>(
                viewModel.Workspace.Document.Entities.GetRequired(viewModel.Workspace.SelectionSet.SelectedIds.Single()));
            Assert.Equal(missingFile, selected.FilePath);
        }
        finally
        {
            File.Delete(existingFile);
        }
    }

    [Fact]
    public void RelinkFirstMissingImageReference_ShouldKeepGeometryAndSelectReplacement()
    {
        string missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        string replacementFile = Path.GetTempFileName();

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.AddImageReference(
                missingFile,
                new Point2D(10, 5),
                width: 12,
                height: 9,
                pixelWidth: 120,
                pixelHeight: 90);

            ToolResult result = viewModel.RelinkFirstMissingImageReference(
                replacementFile,
                pixelWidth: 400,
                pixelHeight: 300);

            Assert.True(result.Changed);

            ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
                viewModel.Workspace.Document.Entities.All.Single());

            Assert.Equal(replacementFile, image.FilePath);
            Assert.Equal(new Point2D(10, 5), image.Center);
            Assert.Equal(12, image.Width, precision: 6);
            Assert.Equal(9, image.Height, precision: 6);
            Assert.Equal(400, image.PixelWidth);
            Assert.Equal(300, image.PixelHeight);
            Assert.True(viewModel.HasSingleSelectedImageReference);
        }
        finally
        {
            File.Delete(replacementFile);
        }
    }

    [Fact]
    public void CollectExternalImageReferences_ShouldCopyImagesBesideDrawingAndKeepGeometry()
    {
        string tempRoot = Path.Combine(
            Path.GetTempPath(),
            "OpenCad2D.App.Tests",
            Guid.NewGuid().ToString("N"));
        string sourceFolder = Path.Combine(tempRoot, "source");
        string drawingFolder = Path.Combine(tempRoot, "drawing");
        string sourceImagePath = Path.Combine(sourceFolder, "plan.png");
        string drawingPath = Path.Combine(drawingFolder, "drawing.opencad2d.json");
        string expectedCollectedPath = Path.Combine(drawingFolder, "images", "plan.png");

        try
        {
            Directory.CreateDirectory(sourceFolder);
            Directory.CreateDirectory(drawingFolder);
            File.WriteAllText(sourceImagePath, "fake image bytes");

            var viewModel = new MainWindowViewModel();
            viewModel.AddImageReference(
                sourceImagePath,
                new Point2D(10, 5),
                width: 12,
                height: 9,
                pixelWidth: 120,
                pixelHeight: 90);
            viewModel.SaveToFile(
                drawingPath,
                new ViewportStateDto());

            ToolResult result = viewModel.CollectExternalImageReferences();

            Assert.True(result.Changed);
            Assert.True(File.Exists(expectedCollectedPath));

            ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
                viewModel.Workspace.Document.Entities.All.Single());

            Assert.Equal(Path.GetFullPath(expectedCollectedPath), Path.GetFullPath(image.FilePath));
            Assert.Equal(new Point2D(10, 5), image.Center);
            Assert.Equal(12, image.Width, precision: 6);
            Assert.Equal(9, image.Height, precision: 6);
            Assert.Equal(120, image.PixelWidth);
            Assert.Equal(90, image.PixelHeight);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void CollectExternalImageReferences_ShouldReuseSameCollectedFileForDuplicateSource()
    {
        string tempRoot = Path.Combine(
            Path.GetTempPath(),
            "OpenCad2D.App.Tests",
            Guid.NewGuid().ToString("N"));
        string sourceFolder = Path.Combine(tempRoot, "source");
        string drawingFolder = Path.Combine(tempRoot, "drawing");
        string sourceImagePath = Path.Combine(sourceFolder, "photo.jpg");
        string drawingPath = Path.Combine(drawingFolder, "drawing.opencad2d.json");

        try
        {
            Directory.CreateDirectory(sourceFolder);
            Directory.CreateDirectory(drawingFolder);
            File.WriteAllText(sourceImagePath, "fake image bytes");

            var viewModel = new MainWindowViewModel();
            viewModel.AddImageReference(
                sourceImagePath,
                Point2D.Origin,
                width: 10,
                height: 5,
                pixelWidth: 100,
                pixelHeight: 50);
            viewModel.AddImageReference(
                sourceImagePath,
                new Point2D(20, 10),
                width: 8,
                height: 4,
                pixelWidth: 100,
                pixelHeight: 50);
            viewModel.SaveToFile(
                drawingPath,
                new ViewportStateDto());

            ToolResult result = viewModel.CollectExternalImageReferences();

            Assert.True(result.Changed);
            Assert.Single(Directory.GetFiles(Path.Combine(drawingFolder, "images")));

            string[] imagePaths = viewModel.Workspace.Document.Entities.All
                .OfType<ImageReferenceEntity>()
                .Select(image => Path.GetFullPath(image.FilePath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Single(imagePaths);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void CollectExternalImageReferences_WithoutSavedDrawing_ShouldNotChangeDocument()
    {
        string existingFile = Path.GetTempFileName();

        try
        {
            var viewModel = new MainWindowViewModel();
            viewModel.AddImageReference(
                existingFile,
                Point2D.Origin,
                width: 10,
                height: 5,
                pixelWidth: 100,
                pixelHeight: 50);

            ToolResult result = viewModel.CollectExternalImageReferences();

            Assert.False(result.Changed);
            ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
                viewModel.Workspace.Document.Entities.All.Single());
            Assert.Equal(existingFile, image.FilePath);
        }
        finally
        {
            File.Delete(existingFile);
        }
    }



    [Fact]
    public void SelectImageReference_ShouldSelectRequestedImageReference()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.AddImageReference(
            "first.png",
            Point2D.Origin,
            width: 10,
            height: 5,
            pixelWidth: 100,
            pixelHeight: 50);

        ImageReferenceEntity first = Assert.IsType<ImageReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        viewModel.AddImageReference(
            "second.png",
            new Point2D(20, 10),
            width: 8,
            height: 4,
            pixelWidth: 80,
            pixelHeight: 40);

        ToolResult result = viewModel.SelectImageReference(first.Id);

        Assert.True(result.Changed);
        Assert.Equal(first.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());
    }

    [Fact]
    public void ReplaceImageReference_ById_ShouldKeepGeometryAndSelectReplacement()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.AddImageReference(
            "old.png",
            new Point2D(10, 5),
            width: 12,
            height: 9,
            pixelWidth: 120,
            pixelHeight: 90);

        ImageReferenceEntity original = Assert.IsType<ImageReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        ToolResult result = viewModel.ReplaceImageReference(
            original.Id,
            "new.jpg",
            pixelWidth: 400,
            pixelHeight: 300);

        Assert.True(result.Changed);

        ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        Assert.Equal(original.Id, image.Id);
        Assert.Equal("new.jpg", image.FilePath);
        Assert.Equal(new Point2D(10, 5), image.Center);
        Assert.Equal(12, image.Width, precision: 6);
        Assert.Equal(9, image.Height, precision: 6);
        Assert.Equal(400, image.PixelWidth);
        Assert.Equal(300, image.PixelHeight);
        Assert.Equal(original.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());
    }

    [Fact]
    public void ImageReferenceManagerWindowViewModel_ShouldGroupDuplicateFilePaths()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.AddImageReference(
            "shared.png",
            Point2D.Origin,
            width: 10,
            height: 5,
            pixelWidth: 100,
            pixelHeight: 50);
        viewModel.AddImageReference(
            "shared.png",
            new Point2D(20, 10),
            width: 8,
            height: 4,
            pixelWidth: 100,
            pixelHeight: 50);

        var manager = new ImageReferenceManagerWindowViewModel(
            viewModel.Workspace.Document);

        ImageReferenceItemViewModel reference = Assert.Single(manager.References);
        Assert.Equal(2, reference.InstanceCount);
        Assert.Equal("2", reference.InstanceCountText);
    }

    [Fact]
    public void ImageReferenceManagerWindowViewModel_ShouldSummarizeReferencesAndMissingFiles()
    {
        string existingFile = Path.GetTempFileName();
        string missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

        try
        {
            var viewModel = new MainWindowViewModel();
            viewModel.AddImageReference(
                existingFile,
                Point2D.Origin,
                width: 10,
                height: 5,
                pixelWidth: 100,
                pixelHeight: 50);
            viewModel.AddImageReference(
                missingFile,
                new Point2D(20, 10),
                width: 8,
                height: 4,
                pixelWidth: 80,
                pixelHeight: 40);

            var manager = new ImageReferenceManagerWindowViewModel(
                viewModel.Workspace.Document);

            Assert.Equal(2, manager.ReferenceCount);
            Assert.Equal(1, manager.MissingCount);
            Assert.Contains(manager.References, reference => reference.Exists);
            Assert.Contains(manager.References, reference => reference.IsMissing);
            Assert.Contains("1 missing", manager.SummaryText);
        }
        finally
        {
            File.Delete(existingFile);
        }
    }

}
