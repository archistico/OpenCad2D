using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.Blocks;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelBlockTests
{
    [Fact]
    public void CreateBlockFromSelection_ShouldCreateDefinitionAndReplaceSelectionWithReference()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(10, 20),
            new Point2D(15, 20));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);

        var result = viewModel.CreateBlockFromSelection(
            new CreateBlockOptions("Door", 10, 20));

        Assert.True(result.Changed);
        Assert.Single(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.Single(viewModel.Workspace.Document.Entities.All);

        BlockReferenceEntity reference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(new Point2D(10, 20), reference.InsertionPoint);
        Assert.Equal(reference.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());

        var definition = viewModel.Workspace.Document.BlockDefinitions.GetRequired(reference.BlockDefinitionId);
        LineEntity localLine = Assert.IsType<LineEntity>(definition.Entities.Single());
        Assert.Equal(Point2D.Origin, localLine.Start);
        Assert.Equal(new Point2D(5, 0), localLine.End);
    }

    [Fact]
    public void CreateBlockFromSelection_ShouldRejectDuplicateName()
    {
        var viewModel = new MainWindowViewModel();
        var first = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        var second = new LineEntity(new Point2D(0, 1), new Point2D(1, 1));
        viewModel.Workspace.Document.AddEntity(first);
        viewModel.Workspace.SelectionSet.ReplaceWith(first.Id);
        viewModel.CreateBlockFromSelection(new CreateBlockOptions("Door", 0, 0));

        viewModel.Workspace.Document.AddEntity(second);
        viewModel.Workspace.SelectionSet.ReplaceWith(second.Id);

        var result = viewModel.CreateBlockFromSelection(new CreateBlockOptions("Door", 0, 0));

        Assert.False(result.Changed);
        Assert.Single(viewModel.Workspace.Document.BlockDefinitions.All);
    }

    [Fact]
    public void CreateBlockFromSelection_ShouldBeUndoable()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(3, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);

        viewModel.CreateBlockFromSelection(new CreateBlockOptions("Beam", 0, 0));

        Assert.Single(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.IsType<BlockReferenceEntity>(viewModel.Workspace.Document.Entities.All.Single());

        viewModel.Undo();

        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
        LineEntity restoredLine = Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(line.Id, restoredLine.Id);
    }
}

public sealed class MainWindowViewModelBlockBasePointPickTests
{
    [Fact]
    public void BeginCreateBlockBasePointPick_ShouldStartPendingWorkflow()
    {
        var viewModel = new MainWindowViewModel();

        var result = viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

        Assert.True(viewModel.IsCreateBlockBasePointPickPending);
        Assert.Equal(OpenCad2D.Tools.Common.ToolResultKind.Started, result.Kind);
    }

    [Fact]
    public void CommitCreateBlockBasePointPick_ShouldUsePickedPointAsBasePoint()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(10, 20),
            new Point2D(15, 20));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

        var result = viewModel.CommitCreateBlockBasePointPick(new Point2D(10, 20));

        Assert.True(result.Changed);
        Assert.False(viewModel.IsCreateBlockBasePointPickPending);
        BlockReferenceEntity reference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(new Point2D(10, 20), reference.InsertionPoint);
    }

    [Fact]
    public void CancelCreateBlockBasePointPick_ShouldNotModifyDocument()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

        var result = viewModel.CancelCreateBlockBasePointPick();

        Assert.False(result.Changed);
        Assert.False(viewModel.IsCreateBlockBasePointPickPending);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.All.Single());
    }
}

public sealed class MainWindowViewModelInsertBlockTests
{
    [Fact]
    public void BeginInsertBlockPlacement_ShouldStartPendingWorkflow()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;

        var result = viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));

        Assert.True(viewModel.IsBlockInsertionPending);
        Assert.Equal(OpenCad2D.Tools.Common.ToolResultKind.Started, result.Kind);
    }

    [Fact]
    public void CommitPendingBlockInsertion_ShouldCreateReferenceAtPickedPoint()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 2, 90));

        var result = viewModel.CommitPendingBlockInsertion(new Point2D(10, 20));

        Assert.True(result.Changed);
        Assert.False(viewModel.IsBlockInsertionPending);
        Assert.Equal(2, viewModel.Workspace.Document.Entities.All.OfType<BlockReferenceEntity>().Count());

        BlockReferenceEntity inserted = viewModel.Workspace.Document.Entities.All
            .OfType<BlockReferenceEntity>()
            .Single(reference => reference.Id != originalReference.Id);

        Assert.Equal(definitionId, inserted.BlockDefinitionId);
        Assert.Equal(new Point2D(10, 20), inserted.InsertionPoint);
        Assert.Equal(0, inserted.XAxis.X, 12);
        Assert.Equal(2, inserted.XAxis.Y, 12);
        Assert.Equal(-2, inserted.YAxis.X, 12);
        Assert.Equal(0, inserted.YAxis.Y, 12);
        Assert.Equal(inserted.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());
    }

    [Fact]
    public void CommitPendingBlockInsertion_ShouldBeUndoable()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));
        viewModel.CommitPendingBlockInsertion(new Point2D(10, 20));

        viewModel.Undo();

        BlockReferenceEntity remainingReference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(originalReference.Id, remainingReference.Id);
        Assert.Single(viewModel.Workspace.Document.BlockDefinitions.All);
    }

    [Fact]
    public void CancelPendingBlockInsertion_ShouldNotModifyDocument()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));

        var result = viewModel.CancelPendingBlockInsertion();

        Assert.False(result.Changed);
        Assert.False(viewModel.IsBlockInsertionPending);
        Assert.Single(viewModel.Workspace.Document.Entities.All);
        Assert.Equal(originalReference.Id, viewModel.Workspace.Document.Entities.All.Single().Id);
    }

    private static MainWindowViewModel CreateViewModelWithDoorBlock(
        out BlockReferenceEntity originalReference)
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.CreateBlockFromSelection(new CreateBlockOptions("Door", 0, 0));

        originalReference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        return viewModel;
    }
}

public sealed class MainWindowViewModelBlockManagerTests
{
    [Fact]
    public void ApplyBlockDefinitionChanges_ShouldRenameBlockAndBeUndoable()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.CreateBlockFromSelection(new CreateBlockOptions("Door", 0, 0));
        var definition = viewModel.Workspace.Document.BlockDefinitions.All.Single();

        var result = viewModel.ApplyBlockDefinitionChanges(new[]
        {
            definition.WithName("Door Renamed")
        });

        Assert.True(result.Changed);
        Assert.Equal("Door Renamed", viewModel.Workspace.Document.BlockDefinitions.GetRequired(definition.Id).Name);

        viewModel.Undo();

        Assert.Equal("Door", viewModel.Workspace.Document.BlockDefinitions.GetRequired(definition.Id).Name);
    }
}
