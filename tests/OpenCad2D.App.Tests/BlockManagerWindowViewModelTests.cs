using OpenCad2D.App.ViewModels.Blocks;
using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class BlockManagerWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldListDefinitionsWithReferenceCounts()
    {
        var document = new CadDocument();
        var used = CreateDefinition("door", "Door");
        var unused = CreateDefinition("window", "Window");
        document.BlockDefinitions.Add(used);
        document.BlockDefinitions.Add(unused);
        document.AddEntity(new BlockReferenceEntity(
            used.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            used.GetBoundingBox()));

        var viewModel = new BlockManagerWindowViewModel(document);

        Assert.Equal(2, viewModel.Blocks.Count);
        EditableBlockDefinitionViewModel usedBlock = viewModel.Blocks.Single(block => block.Id == used.Id);
        EditableBlockDefinitionViewModel unusedBlock = viewModel.Blocks.Single(block => block.Id == unused.Id);
        Assert.Equal(1, usedBlock.InstanceCount);
        Assert.Equal(0, usedBlock.NestedReferenceCount);
        Assert.Equal(1, usedBlock.TotalReferenceCount);
        Assert.Equal(0, unusedBlock.TotalReferenceCount);
    }

    [Fact]
    public void Constructor_ShouldCountNestedBlockReferences()
    {
        var document = new CadDocument();
        var child = CreateDefinition("child", "Child");
        var parent = new BlockDefinition(
            new BlockDefinitionId("parent"),
            "Parent",
            new[]
            {
                new BlockReferenceEntity(
                    child.Id,
                    Point2D.Origin,
                    new Vector2D(1, 0),
                    new Vector2D(0, 1),
                    child.GetBoundingBox())
            });
        document.BlockDefinitions.Add(child);
        document.BlockDefinitions.Add(parent);

        var viewModel = new BlockManagerWindowViewModel(document);

        EditableBlockDefinitionViewModel childBlock = viewModel.Blocks.Single(block => block.Id == child.Id);
        EditableBlockDefinitionViewModel parentBlock = viewModel.Blocks.Single(block => block.Id == parent.Id);
        Assert.Equal(0, childBlock.InstanceCount);
        Assert.Equal(1, childBlock.NestedReferenceCount);
        Assert.Equal(1, childBlock.TotalReferenceCount);
        Assert.False(childBlock.CanDelete);
        Assert.True(parentBlock.ContainsNestedBlockReferences);
    }

    [Fact]
    public void Constructor_ShouldExposeDocumentMissingReferenceDiagnostics()
    {
        var document = new CadDocument();
        document.Entities.Add(new BlockReferenceEntity(
            new BlockDefinitionId("missing"),
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            new BoundingBox2D(0, 0, 1, 1)));

        var viewModel = new BlockManagerWindowViewModel(document);

        Assert.True(viewModel.HasDocumentDiagnostics);
        Assert.Equal(1, viewModel.MissingDrawingReferenceCount);
        Assert.Contains("missing definition", viewModel.DocumentDiagnosticsText);
        Assert.Contains("diagnostics", viewModel.SummaryText);
    }

    [Fact]
    public void Constructor_ShouldMarkEmptyDefinitionAsDiagnosticIssue()
    {
        var document = new CadDocument();
        var definition = new BlockDefinition(
            new BlockDefinitionId("empty"),
            "Empty",
            Array.Empty<CadEntity>());
        document.BlockDefinitions.Add(definition);

        var viewModel = new BlockManagerWindowViewModel(document);

        EditableBlockDefinitionViewModel block = viewModel.Blocks.Single();
        Assert.True(block.IsEmpty);
        Assert.True(block.HasDiagnosticIssue);
        Assert.Equal("Empty", block.StatusText);
        Assert.Contains("empty definition", block.DiagnosticText);
    }

    [Fact]
    public void Constructor_ShouldMarkRecursiveDefinitionAsBlockingDiagnostic()
    {
        var document = new CadDocument();
        var definitionId = new BlockDefinitionId("recursive");
        var definition = new BlockDefinition(
            definitionId,
            "Recursive",
            new[]
            {
                new BlockReferenceEntity(
                    definitionId,
                    Point2D.Origin,
                    new Vector2D(1, 0),
                    new Vector2D(0, 1),
                    new BoundingBox2D(0, 0, 1, 1))
            });
        document.BlockDefinitions.Add(definition);

        var viewModel = new BlockManagerWindowViewModel(document);

        EditableBlockDefinitionViewModel block = viewModel.Blocks.Single();
        Assert.True(block.HasSelfReference);
        Assert.True(block.HasRecursiveReference);
        Assert.True(block.HasBlockingDiagnostic);
        Assert.False(block.CanDelete);
        Assert.Equal("Recursive", block.StatusText);
    }

    [Fact]
    public void DeleteSelectedBlock_ShouldRemoveOnlyUnusedDefinition()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("window", "Window");
        document.BlockDefinitions.Add(definition);
        var viewModel = new BlockManagerWindowViewModel(document);
        viewModel.SelectedBlock = viewModel.Blocks.Single();

        viewModel.DeleteSelectedBlock();

        Assert.Empty(viewModel.Blocks);
    }

    [Fact]
    public void DeleteSelectedBlock_ShouldRejectUsedDefinition()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("door", "Door");
        document.BlockDefinitions.Add(definition);
        document.AddEntity(new BlockReferenceEntity(
            definition.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            definition.GetBoundingBox()));
        var viewModel = new BlockManagerWindowViewModel(document);
        viewModel.SelectedBlock = viewModel.Blocks.Single();

        viewModel.DeleteSelectedBlock();

        Assert.Single(viewModel.Blocks);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void DeleteSelectedBlock_ShouldRejectNestedUsedDefinition()
    {
        var document = new CadDocument();
        var child = CreateDefinition("child", "Child");
        var parent = new BlockDefinition(
            new BlockDefinitionId("parent"),
            "Parent",
            new[]
            {
                new BlockReferenceEntity(
                    child.Id,
                    Point2D.Origin,
                    new Vector2D(1, 0),
                    new Vector2D(0, 1),
                    child.GetBoundingBox())
            });
        document.BlockDefinitions.Add(child);
        document.BlockDefinitions.Add(parent);
        var viewModel = new BlockManagerWindowViewModel(document);
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Id == child.Id);

        viewModel.DeleteSelectedBlock();

        Assert.Equal(2, viewModel.Blocks.Count);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void TryBuildResult_ShouldRejectDuplicateNames()
    {
        var document = new CadDocument();
        document.BlockDefinitions.Add(CreateDefinition("door", "Door"));
        document.BlockDefinitions.Add(CreateDefinition("window", "Window"));
        var viewModel = new BlockManagerWindowViewModel(document);
        foreach (EditableBlockDefinitionViewModel block in viewModel.Blocks)
        {
            block.Name = "Duplicate";
        }

        bool valid = viewModel.TryBuildResult(
            BlockManagerAction.Close,
            out _);

        Assert.False(valid);
        Assert.True(viewModel.HasValidationMessage);
    }


    [Fact]
    public void RenamingBlock_ShouldExposePendingRenameSummary()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("door", "Door");
        document.BlockDefinitions.Add(definition);
        var viewModel = new BlockManagerWindowViewModel(document);
        EditableBlockDefinitionViewModel block = viewModel.Blocks.Single();

        block.Name = "Main Door";

        Assert.True(block.IsRenamed);
        Assert.Equal(1, viewModel.PendingRenameCount);
        Assert.True(viewModel.HasPendingRenames);
        Assert.True(viewModel.CanResetBlockNames);
        Assert.Contains("1 pending block rename", viewModel.RenameSummaryText);
        Assert.Contains("1 pending rename", viewModel.SummaryText);
        Assert.Contains("Renamed from 'Door'", viewModel.SelectedBlockDetailsText);
    }

    [Fact]
    public void ResetBlockNames_ShouldRestoreOriginalNamesAndClearRenameSummary()
    {
        var document = new CadDocument();
        document.BlockDefinitions.Add(CreateDefinition("door", "Door"));
        document.BlockDefinitions.Add(CreateDefinition("window", "Window"));
        var viewModel = new BlockManagerWindowViewModel(document);

        foreach (EditableBlockDefinitionViewModel block in viewModel.Blocks)
        {
            block.Name += " Changed";
        }

        Assert.Equal(2, viewModel.PendingRenameCount);

        viewModel.ResetBlockNames();

        Assert.Equal(0, viewModel.PendingRenameCount);
        Assert.False(viewModel.HasPendingRenames);
        Assert.False(viewModel.CanResetBlockNames);
        Assert.Contains(viewModel.Blocks, block => block.Name == "Door");
        Assert.Contains(viewModel.Blocks, block => block.Name == "Window");
        Assert.DoesNotContain("pending rename", viewModel.SummaryText);
    }


    [Fact]
    public void ResetBlockNames_AfterDuplicate_ShouldRestoreOriginalSourceName()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("door", "Door");
        document.BlockDefinitions.Add(definition);
        var viewModel = new BlockManagerWindowViewModel(document);
        EditableBlockDefinitionViewModel block = viewModel.Blocks.Single();
        block.Name = "Main Door";
        viewModel.SelectedBlock = block;

        viewModel.DuplicateSelectedBlock();
        viewModel.ResetBlockNames();

        EditableBlockDefinitionViewModel original = viewModel.Blocks.Single(item => item.Id == definition.Id);
        EditableBlockDefinitionViewModel duplicate = viewModel.Blocks.Single(item => item.Id != definition.Id);
        Assert.Equal("Door", original.Name);
        Assert.Equal("Main Door Copy", duplicate.Name);
        Assert.Equal(0, viewModel.PendingRenameCount);
    }

    [Fact]
    public void TryBuildResult_ShouldReturnTrimmedRenamedDefinition()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("door", "Door");
        document.BlockDefinitions.Add(definition);
        var viewModel = new BlockManagerWindowViewModel(document);
        viewModel.Blocks.Single().Name = "  Main Door  ";

        bool valid = viewModel.TryBuildResult(
            BlockManagerAction.Close,
            out BlockManagerResult result);

        Assert.True(valid);
        Assert.Equal("Main Door", result.BlockDefinitions.Single().Name);
    }

    [Fact]
    public void TryBuildResult_ShouldRejectRecursiveDefinition()
    {
        var document = new CadDocument();
        var definitionId = new BlockDefinitionId("recursive");
        document.BlockDefinitions.Add(new BlockDefinition(
            definitionId,
            "Recursive",
            new[]
            {
                new BlockReferenceEntity(
                    definitionId,
                    Point2D.Origin,
                    new Vector2D(1, 0),
                    new Vector2D(0, 1),
                    new BoundingBox2D(0, 0, 1, 1))
            }));
        var viewModel = new BlockManagerWindowViewModel(document);

        bool valid = viewModel.TryBuildResult(
            BlockManagerAction.Close,
            out _);

        Assert.False(valid);
        Assert.True(viewModel.HasValidationMessage);
    }


    [Fact]
    public void DuplicateSelectedBlock_ShouldCreateIndependentDefinitionWithUniqueName()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("door", "Door");
        document.BlockDefinitions.Add(definition);
        document.AddEntity(new BlockReferenceEntity(
            definition.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            definition.GetBoundingBox()));
        var viewModel = new BlockManagerWindowViewModel(document);
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Id == definition.Id);

        viewModel.DuplicateSelectedBlock();

        Assert.Equal(2, viewModel.Blocks.Count);
        EditableBlockDefinitionViewModel original = viewModel.Blocks.Single(block => block.Id == definition.Id);
        EditableBlockDefinitionViewModel duplicate = viewModel.Blocks.Single(block => block.Id != definition.Id);
        Assert.Equal("Door Copy", duplicate.Name);
        Assert.Equal(1, original.InstanceCount);
        Assert.Equal(0, duplicate.InstanceCount);
        Assert.NotEqual(
            definition.Entities[0].Id,
            duplicate.Definition.Entities[0].Id);
        Assert.Same(duplicate, viewModel.SelectedBlock);
    }

    [Fact]
    public void DuplicateSelectedBlock_ShouldRejectBlockingDiagnosticDefinition()
    {
        var document = new CadDocument();
        var definitionId = new BlockDefinitionId("recursive");
        var definition = new BlockDefinition(
            definitionId,
            "Recursive",
            new[]
            {
                new BlockReferenceEntity(
                    definitionId,
                    Point2D.Origin,
                    new Vector2D(1, 0),
                    new Vector2D(0, 1),
                    new BoundingBox2D(0, 0, 1, 1))
            });
        document.BlockDefinitions.Add(definition);
        var viewModel = new BlockManagerWindowViewModel(document);
        viewModel.SelectedBlock = viewModel.Blocks.Single();

        viewModel.DuplicateSelectedBlock();

        Assert.Single(viewModel.Blocks);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void PurgeUnusedBlocks_ShouldRemoveDrawingUnreachableBlockTree()
    {
        var document = new CadDocument();
        var used = CreateDefinition("used", "Used");
        var child = CreateDefinition("child", "Child");
        var parent = new BlockDefinition(
            new BlockDefinitionId("parent"),
            "Parent",
            new[]
            {
                new BlockReferenceEntity(
                    child.Id,
                    Point2D.Origin,
                    new Vector2D(1, 0),
                    new Vector2D(0, 1),
                    child.GetBoundingBox())
            });
        document.BlockDefinitions.Add(used);
        document.BlockDefinitions.Add(child);
        document.BlockDefinitions.Add(parent);
        document.AddEntity(new BlockReferenceEntity(
            used.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            used.GetBoundingBox()));
        var viewModel = new BlockManagerWindowViewModel(document);

        viewModel.PurgeUnusedBlocks();

        Assert.Single(viewModel.Blocks);
        Assert.Equal(used.Id, viewModel.Blocks.Single().Id);
        Assert.Equal(0, viewModel.PurgeCandidateCount);
    }

    [Fact]
    public void PurgeUnusedBlocks_ShouldKeepNestedDefinitionsReachableFromDrawing()
    {
        var document = new CadDocument();
        var child = CreateDefinition("child", "Child");
        var parent = new BlockDefinition(
            new BlockDefinitionId("parent"),
            "Parent",
            new[]
            {
                new BlockReferenceEntity(
                    child.Id,
                    Point2D.Origin,
                    new Vector2D(1, 0),
                    new Vector2D(0, 1),
                    child.GetBoundingBox())
            });
        document.BlockDefinitions.Add(child);
        document.BlockDefinitions.Add(parent);
        document.AddEntity(new BlockReferenceEntity(
            parent.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            parent.GetBoundingBox()));
        var viewModel = new BlockManagerWindowViewModel(document);

        viewModel.PurgeUnusedBlocks();

        Assert.Equal(2, viewModel.Blocks.Count);
        Assert.Contains(viewModel.Blocks, block => block.Id == child.Id);
        Assert.Contains(viewModel.Blocks, block => block.Id == parent.Id);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void TryBuildResult_ShouldReturnDuplicatedAndPurgedDefinitionsAsOneUpdate()
    {
        var document = new CadDocument();
        var source = CreateDefinition("source", "Source");
        var unused = CreateDefinition("unused", "Unused");
        document.BlockDefinitions.Add(source);
        document.BlockDefinitions.Add(unused);
        document.AddEntity(new BlockReferenceEntity(
            source.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            source.GetBoundingBox()));
        var viewModel = new BlockManagerWindowViewModel(document);
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Id == source.Id);
        viewModel.DuplicateSelectedBlock();
        BlockDefinitionId duplicateId = viewModel.SelectedBlock!.Id;
        Assert.True(viewModel.TryBuildResult(
            BlockManagerAction.Close,
            out BlockManagerResult duplicatedResult));
        document.BlockDefinitions.ReplaceAll(duplicatedResult.BlockDefinitions);
        document.AddEntity(new BlockReferenceEntity(
            duplicateId,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            source.GetBoundingBox()));

        // Re-open the manager state against the drawing references so the duplicate is now reachable.
        viewModel = new BlockManagerWindowViewModel(document);
        viewModel.PurgeUnusedBlocks();

        bool valid = viewModel.TryBuildResult(
            BlockManagerAction.Close,
            out BlockManagerResult result);

        Assert.True(valid);
        Assert.Equal(2, result.BlockDefinitions.Count);
        Assert.DoesNotContain(result.BlockDefinitions, definition => definition.Id == unused.Id);
        Assert.Contains(result.BlockDefinitions, definition => definition.Id == source.Id);
        Assert.Contains(result.BlockDefinitions, definition => definition.Id == duplicateId);
    }

    private static BlockDefinition CreateDefinition(
        string id,
        string name)
    {
        return new BlockDefinition(
            new BlockDefinitionId(id),
            name,
            new[]
            {
                new LineEntity(Point2D.Origin, new Point2D(1, 0))
            });
    }
}
