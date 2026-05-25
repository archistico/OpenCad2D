using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelOpenCad2DImportTests
{
    [Fact]
    public void ImportDrawingFromFile_ShouldAppendEntitiesAndKeepCurrentDocumentFilePath()
    {
        string filePath = CreateTemporaryOpenCad2DDrawing(document =>
        {
            document.AddEntity(new LineEntity(
                new Point2D(0, 0),
                new Point2D(10, 0),
                id: new EntityId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))));
        });

        try
        {
            var viewModel = new MainWindowViewModel();

            var result = viewModel.ImportDrawingFromFile(filePath);

            Assert.True(result.Changed);
            Assert.Single(viewModel.Workspace.Document.Entities.All);

            LineEntity importedLine = Assert.IsType<LineEntity>(
                viewModel.Workspace.Document.Entities.All.Single());

            Assert.NotEqual(
                new EntityId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                importedLine.Id);
            Assert.Null(viewModel.CurrentFilePath);
            Assert.True(viewModel.IsDirty);
            Assert.Contains("Imported OpenCad2D drawing", viewModel.LastMessage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ImportDrawingFromFile_ShouldMergeMissingLayersAndSelectImportedEntities()
    {
        string filePath = CreateTemporaryOpenCad2DDrawing(document =>
        {
            var importedLayerId = new LayerId("ImportedLayer");
            document.Layers.Add(new Layer(
                importedLayerId,
                "Imported Layer",
                LineFormatId.Continuous));
            document.AddEntity(new CircleEntity(
                new Point2D(5, 5),
                3,
                layerId: importedLayerId));
        });

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.ImportDrawingFromFile(filePath);

            Assert.Contains(
                viewModel.Workspace.Document.Layers.All,
                layer => layer.Id == new LayerId("ImportedLayer"));

            CircleEntity circle = Assert.IsType<CircleEntity>(
                viewModel.Workspace.Document.Entities.All.Single());
            Assert.Equal(new LayerId("ImportedLayer"), circle.LayerId);
            Assert.Equal(circle.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ImportDrawingFromFile_ShouldBeUndoable()
    {
        string filePath = CreateTemporaryOpenCad2DDrawing(document =>
        {
            document.AddEntity(new LineEntity(
                Point2D.Origin,
                new Point2D(20, 0)));
        });

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.ImportDrawingFromFile(filePath);
            Assert.Single(viewModel.Workspace.Document.Entities.All);

            viewModel.Undo();

            Assert.Empty(viewModel.Workspace.Document.Entities.All);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTemporaryOpenCad2DDrawing(Action<CadDocument> configure)
    {
        var document = new CadDocument();
        configure(document);

        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-import-{Guid.NewGuid():N}.opencad2d.json");

        var serializer = new JsonDocumentSerializer();
        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        serializer.SaveToFile(
            dto,
            filePath);

        return filePath;
    }
}
