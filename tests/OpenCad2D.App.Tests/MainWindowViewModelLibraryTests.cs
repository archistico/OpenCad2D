using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.Library;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelLibraryTests
{
    [Fact]
    public void BeginInsertLibraryItem_ShouldStartPendingWorkflowWithoutChangingDocument()
    {
        var viewModel = new MainWindowViewModel();
        LibraryCatalogItem item = CreateLibraryItem("arredo", "chair");

        var result = viewModel.BeginInsertLibraryItem(item);

        Assert.True(viewModel.IsLibraryInsertionPending);
        Assert.Equal(OpenCad2D.Tools.Common.ToolResultKind.Started, result.Kind);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.Empty(viewModel.Workspace.Document.Entities.All);
    }

    [Fact]
    public void CommitPendingLibraryInsertion_ShouldCreateDefinitionAndReference()
    {
        var viewModel = new MainWindowViewModel();
        LibraryCatalogItem item = CreateLibraryItem("arredo", "chair");
        viewModel.BeginInsertLibraryItem(item);

        var result = viewModel.CommitPendingLibraryInsertion(new Point2D(10, 20));

        Assert.True(result.Changed);
        Assert.False(viewModel.IsLibraryInsertionPending);

        BlockReferenceEntity reference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(new Point2D(10, 20), reference.InsertionPoint);
        Assert.Equal(new BlockDefinitionId("Library.arredo.chair"), reference.BlockDefinitionId);
        Assert.Equal(reference.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());

        var definition = viewModel.Workspace.Document.BlockDefinitions.GetRequired(reference.BlockDefinitionId);
        Assert.Equal("Library/arredo/chair", definition.Name);
        Assert.IsType<LineEntity>(definition.Entities.Single());
    }

    [Fact]
    public void CommitPendingLibraryInsertion_ShouldBeUndoableAsSingleStep()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.BeginInsertLibraryItem(CreateLibraryItem("arredo", "chair"));
        viewModel.CommitPendingLibraryInsertion(new Point2D(10, 20));

        viewModel.Undo();

        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
    }

    [Fact]
    public void CancelPendingLibraryInsertion_ShouldNotModifyDocument()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.BeginInsertLibraryItem(CreateLibraryItem("arredo", "chair"));

        var result = viewModel.CancelPendingLibraryInsertion();

        Assert.False(result.Changed);
        Assert.False(viewModel.IsLibraryInsertionPending);
        Assert.Empty(viewModel.Workspace.Document.Entities.All);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
    }

    [Fact]
    public void CommitPendingLibraryInsertion_WhenDefinitionAlreadyExists_ShouldReuseDefinition()
    {
        var viewModel = new MainWindowViewModel();
        LibraryCatalogItem item = CreateLibraryItem("arredo", "chair");
        viewModel.BeginInsertLibraryItem(item);
        viewModel.CommitPendingLibraryInsertion(new Point2D(0, 0));

        viewModel.BeginInsertLibraryItem(item);
        viewModel.CommitPendingLibraryInsertion(new Point2D(10, 0));

        Assert.Single(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.Equal(2, viewModel.Workspace.Document.Entities.All.OfType<BlockReferenceEntity>().Count());
    }

    [Fact]
    public void BeginInsertLibraryItem_WhenItemContainsBlockReference_ShouldReject()
    {
        var viewModel = new MainWindowViewModel();
        LibraryCatalogItem item = CreateLibraryItemWithBlockReference();

        var result = viewModel.BeginInsertLibraryItem(item);

        Assert.False(result.Changed);
        Assert.False(viewModel.IsLibraryInsertionPending);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
    }

    private static LibraryCatalogItem CreateLibraryItem(
        string category,
        string title)
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            Point2D.Origin,
            new Point2D(5, 0)));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        return CreateItem(category, title, dto);
    }

    private static LibraryCatalogItem CreateLibraryItemWithBlockReference()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        var definition = new OpenCad2D.Core.Blocks.BlockDefinition(
            new BlockDefinitionId("Nested"),
            "Nested",
            new[] { new LineEntity(Point2D.Origin, new Point2D(1, 0)) });
        document.BlockDefinitions.Add(definition);
        document.AddEntity(new BlockReferenceEntity(
            definition.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            definition.GetBoundingBox()));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        return CreateItem("arredo", "nested", dto);
    }

    private static LibraryCatalogItem CreateItem(
        string category,
        string title,
        DocumentDto dto)
    {
        return new LibraryCatalogItem(
            $"{category}.{title}",
            title,
            category,
            Path.Combine("library", category, $"{title}.opencad2d.json"),
            Path.Combine(category, $"{title}.opencad2d.json"),
            dto);
    }
}
