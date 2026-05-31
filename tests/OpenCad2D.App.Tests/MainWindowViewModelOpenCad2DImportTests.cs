using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.ImportDrawing;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;
using OpenCad2D.Core.Styling;

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



    [Fact]
    public void ImportDrawingMerger_ShouldReuseEquivalentResourcesByName()
    {
        var target = new CadDocument();
        target.Layers.Add(new Layer(
            new LayerId("Annotations"),
            "Annotations",
            LineFormatId.Annotations));
        int initialLineFormatCount = target.LineFormats.Count;
        int initialLayerCount = target.Layers.Count;

        var source = new CadDocument();
        var sourceLineFormatId = new LineFormatId("ImportedAnnotationsFormat");
        var sourceLayerId = new LayerId("ImportedAnnotationsLayer");

        source.LineFormats.ReplaceAll(new[]
        {
            source.LineFormats.GetById(LineFormatId.Continuous),
            new LineFormat(
                sourceLineFormatId,
                "Annotations",
                CadColor.FromRgb(160, 160, 160),
                LineWeight.FromMillimeters(0.8),
                LineStyle.Continuous)
        });

        source.Layers.Add(new Layer(
            sourceLayerId,
            "Annotations",
            sourceLineFormatId));
        source.AddEntity(new LineEntity(
            Point2D.Origin,
            new Point2D(10, 0),
            layerId: sourceLayerId));

        var merger = new OpenCad2DImportMerger();
        OpenCad2DImportMergeResult result = merger.Merge(target, source);

        Assert.Equal(0, result.AddedLineFormatCount);
        Assert.Equal(0, result.AddedLayerCount);
        Assert.Equal(initialLineFormatCount, target.LineFormats.Count);
        Assert.Equal(initialLayerCount, target.Layers.Count);

        result.Command.Execute(target);

        Assert.Equal(initialLineFormatCount, target.LineFormats.Count);
        Assert.Equal(initialLayerCount, target.Layers.Count);

        LineEntity importedLine = Assert.IsType<LineEntity>(
            target.Entities.All.Single());
        Assert.Equal(new LayerId("Annotations"), importedLine.LayerId);
        Assert.DoesNotContain(
            target.Layers.All,
            layer => layer.Name == "Annotations" && layer.Id != new LayerId("Annotations"));
    }

    [Fact]
    public void ImportDrawingFromFile_WithPlacementOptions_ShouldScaleRotateAndTranslateImportedEntities()
    {
        string filePath = CreateTemporaryOpenCad2DDrawing(document =>
        {
            document.AddEntity(new LineEntity(
                new Point2D(1, 0),
                new Point2D(2, 0)));
        });

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.ImportDrawingFromFile(
                filePath,
                new Point2D(10, 20),
                new OpenCad2DImportPlacementOptions(2, 90));

            LineEntity importedLine = Assert.IsType<LineEntity>(
                viewModel.Workspace.Document.Entities.All.Single());

            AssertNearlyEqual(new Point2D(10, 22), importedLine.Start);
            AssertNearlyEqual(new Point2D(10, 24), importedLine.End);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void PendingImportDrawing_ShouldCommitAtInsertionPointAndBeUndoable()
    {
        string filePath = CreateTemporaryOpenCad2DDrawing(document =>
        {
            document.AddEntity(new CircleEntity(
                new Point2D(2, 3),
                4));
        });

        try
        {
            var viewModel = new MainWindowViewModel();

            var beginResult = viewModel.BeginImportDrawingPlacementFromFile(
                filePath,
                new OpenCad2DImportPlacementOptions(1, 0));

            Assert.True(beginResult.Changed);
            Assert.True(viewModel.IsImportDrawingPlacementPending);
            Assert.Empty(viewModel.Workspace.Document.Entities.All);

            var commitResult = viewModel.CommitPendingImportDrawing(new Point2D(10, 20));

            Assert.True(commitResult.Changed);
            Assert.False(viewModel.IsImportDrawingPlacementPending);

            CircleEntity importedCircle = Assert.IsType<CircleEntity>(
                viewModel.Workspace.Document.Entities.All.Single());
            AssertNearlyEqual(new Point2D(12, 23), importedCircle.Center);

            viewModel.Undo();

            Assert.Empty(viewModel.Workspace.Document.Entities.All);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void PendingImportDrawing_ShouldCancelWithoutChangingDocument()
    {
        string filePath = CreateTemporaryOpenCad2DDrawing(document =>
        {
            document.AddEntity(new LineEntity(
                Point2D.Origin,
                new Point2D(5, 0)));
        });

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.BeginImportDrawingPlacementFromFile(filePath);
            var result = viewModel.CancelPendingImportDrawing();

            Assert.True(result.Changed);
            Assert.False(viewModel.IsImportDrawingPlacementPending);
            Assert.Empty(viewModel.Workspace.Document.Entities.All);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void CommandHudInput_PendingImportDrawing_ShouldAcceptCoordinateFields()
    {
        string filePath = CreateTemporaryOpenCad2DDrawing(document =>
        {
            document.AddEntity(new CircleEntity(
                new Point2D(2, 3),
                4));
        });

        try
        {
            var viewModel = new MainWindowViewModel();

            viewModel.BeginImportDrawingPlacementFromFile(
                filePath,
                new OpenCad2DImportPlacementOptions(1, 0));

            Assert.True(viewModel.IsCommandHudVisible);
            Assert.Equal("Import Drawing", viewModel.CommandHudState.ToolName);
            Assert.Equal("IMPORTDRAWING", viewModel.CurrentPromptState.CommandName);
            Assert.Equal(new[] { CommandHudFieldKind.X, CommandHudFieldKind.Y }, GetEditableHudFieldKinds(viewModel));

            Assert.True(viewModel.TryCommitCommandHudFieldInput(
                CommandHudFieldKind.X,
                "10",
                confirm: false,
                out _));
            Assert.True(viewModel.TryCommitCommandHudFieldInput(
                CommandHudFieldKind.Y,
                "20",
                confirm: true,
                out var result));

            Assert.True(result.Changed);
            Assert.False(viewModel.IsImportDrawingPlacementPending);
            Assert.False(viewModel.IsCommandHudVisible);

            CircleEntity importedCircle = Assert.IsType<CircleEntity>(
                viewModel.Workspace.Document.Entities.All.Single());
            AssertNearlyEqual(new Point2D(12, 23), importedCircle.Center);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static CommandHudFieldKind[] GetEditableHudFieldKinds(MainWindowViewModel viewModel)
    {
        return viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();
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

    private static void AssertNearlyEqual(
        Point2D expected,
        Point2D actual)
    {
        Assert.Equal(expected.X, actual.X, 6);
        Assert.Equal(expected.Y, actual.Y, 6);
    }

}
