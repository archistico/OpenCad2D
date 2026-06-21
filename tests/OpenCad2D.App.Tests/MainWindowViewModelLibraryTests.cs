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
    public void CommitPendingLibraryInsertion_WhenSameItemIdHasDifferentDefinition_ShouldCreateSafeRenamedDefinition()
    {
        var viewModel = new MainWindowViewModel();
        LibraryCatalogItem firstItem = CreateLibraryItem("arredo", "chair", new Point2D(5, 0));
        LibraryCatalogItem changedItem = CreateLibraryItem("arredo", "chair", new Point2D(7, 0));

        viewModel.BeginInsertLibraryItem(firstItem);
        viewModel.CommitPendingLibraryInsertion(new Point2D(0, 0));
        viewModel.BeginInsertLibraryItem(changedItem);
        viewModel.CommitPendingLibraryInsertion(new Point2D(10, 0));

        Assert.Equal(2, viewModel.Workspace.Document.BlockDefinitions.Count);

        BlockReferenceEntity[] references = viewModel.Workspace.Document.Entities.All
            .OfType<BlockReferenceEntity>()
            .OrderBy(reference => reference.InsertionPoint.X)
            .ToArray();
        Assert.Equal(new BlockDefinitionId("Library.arredo.chair"), references[0].BlockDefinitionId);
        Assert.Equal(new BlockDefinitionId("Library.arredo.chair_2"), references[1].BlockDefinitionId);

        var changedDefinition = viewModel.Workspace.Document.BlockDefinitions.GetRequired(references[1].BlockDefinitionId);
        Assert.Equal("Library/arredo/chair (2)", changedDefinition.Name);
        LineEntity line = Assert.IsType<LineEntity>(changedDefinition.Entities.Single());
        Assert.Equal(new Point2D(7, 0), line.End);
    }

    [Fact]
    public void CommitPendingLibraryInsertion_WhenSameNameAndSameDefinitionHasDifferentItemId_ShouldReuseExistingDefinition()
    {
        var viewModel = new MainWindowViewModel();
        LibraryCatalogItem firstItem = CreateLibraryItem("arredo", "chair", new Point2D(5, 0));
        LibraryCatalogItem sameNamedItem = CreateLibraryItem("arredo.alt-chair", "arredo", "chair", new Point2D(5, 0));

        viewModel.BeginInsertLibraryItem(firstItem);
        viewModel.CommitPendingLibraryInsertion(new Point2D(0, 0));
        viewModel.BeginInsertLibraryItem(sameNamedItem);
        viewModel.CommitPendingLibraryInsertion(new Point2D(10, 0));

        Assert.Single(viewModel.Workspace.Document.BlockDefinitions.All);
        BlockReferenceEntity[] references = viewModel.Workspace.Document.Entities.All
            .OfType<BlockReferenceEntity>()
            .OrderBy(reference => reference.InsertionPoint.X)
            .ToArray();
        Assert.Equal(references[0].BlockDefinitionId, references[1].BlockDefinitionId);
        Assert.Equal(new BlockDefinitionId("Library.arredo.chair"), references[1].BlockDefinitionId);
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

    [Fact]
    public void CommandHudInput_LibraryInsertion_ShouldAcceptCoordinateFields()
    {
        var viewModel = new MainWindowViewModel();
        LibraryCatalogItem item = CreateLibraryItem("arredo", "chair");

        viewModel.BeginInsertLibraryItem(item);

        Assert.True(viewModel.IsCommandHudVisible);
        Assert.Equal("Library", viewModel.CommandHudState.ToolName);
        Assert.Equal("LIBRARY", viewModel.CurrentPromptState.CommandName);
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
        Assert.False(viewModel.IsLibraryInsertionPending);
        Assert.False(viewModel.IsCommandHudVisible);

        BlockReferenceEntity reference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(new Point2D(10, 20), reference.InsertionPoint);
    }

    private static CommandHudFieldKind[] GetEditableHudFieldKinds(MainWindowViewModel viewModel)
    {
        return viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();
    }

    private static LibraryCatalogItem CreateLibraryItem(
        string category,
        string title)
    {
        return CreateLibraryItem(category, title, new Point2D(5, 0));
    }

    private static LibraryCatalogItem CreateLibraryItem(
        string category,
        string title,
        Point2D lineEnd)
    {
        return CreateLibraryItem($"{category}.{title}", category, title, lineEnd);
    }

    private static LibraryCatalogItem CreateLibraryItem(
        string id,
        string category,
        string title,
        Point2D lineEnd)
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            Point2D.Origin,
            lineEnd));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        return CreateItem(id, category, title, dto);
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
        return CreateItem($"{category}.{title}", category, title, dto);
    }

    private static LibraryCatalogItem CreateItem(
        string id,
        string category,
        string title,
        DocumentDto dto)
    {
        return new LibraryCatalogItem(
            id,
            title,
            category,
            Path.Combine("library", category, $"{title}.opencad2d.json"),
            Path.Combine(category, $"{title}.opencad2d.json"),
            dto);
    }
}
