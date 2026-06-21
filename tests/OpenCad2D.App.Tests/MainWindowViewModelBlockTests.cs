using OpenCad2D.App.ViewModels;
using OpenCad2D.App.ViewModels.Blocks;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
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

    [Fact]
    public void CreateBlockFromSelection_ShouldRejectEmptySelection()
    {
        var viewModel = new MainWindowViewModel();

        var result = viewModel.CreateBlockFromSelection(new CreateBlockOptions("Empty", 0, 0));

        Assert.False(result.Changed);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.Equal("Select one or more editable entities before creating a block.", viewModel.LastMessage);
    }

    [Fact]
    public void CreateBlockSelectedEntityCount_ShouldTrackSelectedBlockCandidates()
    {
        var viewModel = new MainWindowViewModel();
        var first = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        var second = new LineEntity(new Point2D(0, 1), new Point2D(1, 1));
        viewModel.Workspace.Document.AddEntity(first);
        viewModel.Workspace.Document.AddEntity(second);

        Assert.Equal(0, viewModel.CreateBlockSelectedEntityCount);
        Assert.False(viewModel.CanCreateBlockFromCurrentSelection);

        viewModel.Workspace.SelectionSet.ReplaceWith(new[] { first.Id, second.Id });

        Assert.Equal(2, viewModel.CreateBlockSelectedEntityCount);
        Assert.True(viewModel.CanCreateBlockFromCurrentSelection);
    }
}

public sealed class MainWindowViewModelBlockBasePointPickTests
{
    [Fact]
    public void BeginCreateBlockBasePointPick_ShouldStartPendingWorkflow()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);

        var result = viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

        Assert.True(viewModel.IsCreateBlockBasePointPickPending);
        Assert.True(viewModel.IsCommandHudVisible);
        Assert.Equal("Create Block", viewModel.CommandHudState.ToolName);
        Assert.Equal("CREATEBLOCK", viewModel.CurrentPromptState.CommandName);
        Assert.Equal(new[] { CommandHudFieldKind.X, CommandHudFieldKind.Y }, GetEditableHudFieldKinds(viewModel));
        Assert.Equal(OpenCad2D.Tools.Common.ToolResultKind.Started, result.Kind);
    }

    [Fact]
    public void BeginCreateBlockBasePointPick_ShouldRejectEmptySelection()
    {
        var viewModel = new MainWindowViewModel();

        var result = viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

        Assert.False(result.Changed);
        Assert.False(viewModel.IsCreateBlockBasePointPickPending);
        Assert.False(viewModel.IsCommandHudVisible);
        Assert.Equal("Select one or more editable entities before creating a block.", viewModel.LastMessage);
    }

    [Fact]
    public void CommandHudInput_CreateBlockBasePointPick_ShouldReturnDraftForReview()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(10, 20),
            new Point2D(15, 20));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

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

        Assert.NotNull(result);
        Assert.Equal("Create block 'Door': base point selected. Review options and press OK.", viewModel.LastMessage);
        Assert.False(viewModel.IsCreateBlockBasePointPickPending);
        Assert.False(viewModel.IsCommandHudVisible);
        Assert.Single(viewModel.Workspace.Document.Entities.All);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);

        CreateBlockOptions? completedOptions = viewModel.ConsumeCompletedCreateBlockBasePointPick();

        Assert.NotNull(completedOptions);
        Assert.Equal("Door", completedOptions.Name);
        Assert.Equal(10, completedOptions.BasePointX);
        Assert.Equal(20, completedOptions.BasePointY);
    }

    [Fact]
    public void CompleteCreateBlockBasePointPick_ShouldReturnDraftWithoutCreatingBlock()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(10, 20),
            new Point2D(15, 20));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

        CreateBlockOptions? completedOptions = viewModel.CompleteCreateBlockBasePointPick(
            new Point2D(10, 20),
            out var result);

        Assert.NotNull(completedOptions);
        Assert.Equal(OpenCad2D.Tools.Common.ToolResultKind.Completed, result.Kind);
        Assert.False(viewModel.IsCreateBlockBasePointPickPending);
        Assert.Single(viewModel.Workspace.Document.Entities.All);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.Equal(10, completedOptions.BasePointX);
        Assert.Equal(20, completedOptions.BasePointY);
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

    [Fact]
    public void EscapeCreateBlockBasePointPick_ShouldCancelPendingWorkflow()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));

        var result = viewModel.Escape();

        Assert.NotNull(result);
        Assert.False(viewModel.IsCreateBlockBasePointPickPending);
        Assert.False(viewModel.IsCommandHudVisible);
        Assert.Equal("Create block cancelled.", viewModel.LastMessage);
        Assert.Empty(viewModel.Workspace.Document.BlockDefinitions.All);
        Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.All.Single());
    }


    [Fact]
    public void BeginCreateBlockBasePointPick_ShouldClearStaleHudCoordinateOverrides()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.SetMousePosition(new Point2D(3, 4));

        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _));
        Assert.Equal("10", GetHudField(viewModel, CommandHudFieldKind.X).DisplayValue);

        viewModel.CancelCreateBlockBasePointPick();
        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Window", 0, 0, PickBasePointFromDrawing: true));

        Assert.Equal("3", GetHudField(viewModel, CommandHudFieldKind.X).DisplayValue);
        Assert.Equal("4", GetHudField(viewModel, CommandHudFieldKind.Y).DisplayValue);
    }

    [Fact]
    public void CancelCreateBlockBasePointPick_ShouldClearHudCoordinateOverrides()
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(Point2D.Origin, new Point2D(1, 0));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.SetMousePosition(new Point2D(1, 2));

        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door", 0, 0, PickBasePointFromDrawing: true));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _));

        viewModel.CancelCreateBlockBasePointPick();
        viewModel.BeginCreateBlockBasePointPick(
            new CreateBlockOptions("Door2", 0, 0, PickBasePointFromDrawing: true));

        Assert.Equal("1", GetHudField(viewModel, CommandHudFieldKind.X).DisplayValue);
        Assert.Equal("2", GetHudField(viewModel, CommandHudFieldKind.Y).DisplayValue);
    }

    private static CommandHudFieldViewModel GetHudField(
        MainWindowViewModel viewModel,
        CommandHudFieldKind kind)
    {
        return viewModel.CommandHudState.Fields.Single(field => field.Kind == kind);
    }

    private static CommandHudFieldKind[] GetEditableHudFieldKinds(MainWindowViewModel viewModel)
    {
        return viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();
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
        Assert.True(viewModel.IsCommandHudVisible);
        Assert.Equal("Insert Block", viewModel.CommandHudState.ToolName);
        Assert.Equal("INSERTBLOCK", viewModel.CurrentPromptState.CommandName);
        Assert.Equal(new[] { CommandHudFieldKind.X, CommandHudFieldKind.Y }, GetEditableHudFieldKinds(viewModel));
        Assert.Equal(OpenCad2D.Tools.Common.ToolResultKind.Started, result.Kind);
    }

    [Fact]
    public void CommandHudInput_InsertBlockPlacement_ShouldAcceptCoordinateFields()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 2, 90));

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

        Assert.NotNull(result);
        Assert.Equal("Block 'Door' inserted.", viewModel.LastMessage);
        Assert.False(viewModel.IsBlockInsertionPending);
        Assert.False(viewModel.IsCommandHudVisible);

        BlockReferenceEntity inserted = viewModel.Workspace.Document.Entities.All
            .OfType<BlockReferenceEntity>()
            .Single(reference => reference.Id != originalReference.Id);

        Assert.Equal(definitionId, inserted.BlockDefinitionId);
        Assert.Equal(new Point2D(10, 20), inserted.InsertionPoint);
        Assert.Equal(0, inserted.XAxis.X, 12);
        Assert.Equal(2, inserted.XAxis.Y, 12);
        Assert.Equal(-2, inserted.YAxis.X, 12);
        Assert.Equal(0, inserted.YAxis.Y, 12);
    }

    [Fact]
    public void BeginInsertBlockPlacement_ShouldClearStaleHudCoordinateOverrides()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.SetMousePosition(new Point2D(3, 4));

        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _));
        Assert.Equal("10", GetHudField(viewModel, CommandHudFieldKind.X).DisplayValue);

        viewModel.CancelPendingBlockInsertion();
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));

        Assert.Equal("3", GetHudField(viewModel, CommandHudFieldKind.X).DisplayValue);
        Assert.Equal("4", GetHudField(viewModel, CommandHudFieldKind.Y).DisplayValue);
    }

    [Fact]
    public void CancelPendingBlockInsertion_ShouldClearHudCoordinateOverrides()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.SetMousePosition(new Point2D(1, 2));

        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _));

        viewModel.CancelPendingBlockInsertion();
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));

        Assert.Equal("1", GetHudField(viewModel, CommandHudFieldKind.X).DisplayValue);
        Assert.Equal("2", GetHudField(viewModel, CommandHudFieldKind.Y).DisplayValue);
    }

    [Fact]
    public void CommitPendingBlockInsertion_ShouldClearHudCoordinateOverrides()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.SetMousePosition(new Point2D(5, 6));

        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));
        Assert.True(viewModel.TryCommitCommandHudFieldInput(
            CommandHudFieldKind.X,
            "10",
            confirm: false,
            out _));

        viewModel.CommitPendingBlockInsertion(new Point2D(10, 20));
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));

        Assert.Equal("5", GetHudField(viewModel, CommandHudFieldKind.X).DisplayValue);
        Assert.Equal("6", GetHudField(viewModel, CommandHudFieldKind.Y).DisplayValue);
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

    [Fact]
    public void EscapePendingBlockInsertion_ShouldCancelPendingWorkflow()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        var definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginInsertBlockPlacement(
            new InsertBlockOptions(definitionId, "Door", 1, 0));

        var result = viewModel.Escape();

        Assert.NotNull(result);
        Assert.False(viewModel.IsBlockInsertionPending);
        Assert.False(viewModel.IsCommandHudVisible);
        Assert.Equal("Insert block cancelled.", viewModel.LastMessage);
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

    private static CommandHudFieldViewModel GetHudField(
        MainWindowViewModel viewModel,
        CommandHudFieldKind kind)
    {
        return viewModel.CommandHudState.Fields.Single(field => field.Kind == kind);
    }

    private static CommandHudFieldKind[] GetEditableHudFieldKinds(MainWindowViewModel viewModel)
    {
        return viewModel.CommandHudState.Fields
            .Where(field => field.CanAcceptTypedOverride)
            .Select(field => field.Kind)
            .ToArray();
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

public sealed class MainWindowViewModelBlockEditTests
{
    [Fact]
    public void BeginEditSelectedBlock_ShouldReplaceReferenceWithEditableWorldEntities()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);

        var result = viewModel.BeginEditSelectedBlock();

        Assert.True(result.Changed);
        Assert.True(viewModel.IsBlockEditSessionActive);
        Assert.DoesNotContain(viewModel.Workspace.Document.Entities.All, entity => entity.Id == originalReference.Id);

        LineEntity editLine = Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(new Point2D(10, 20), editLine.Start);
        Assert.Equal(new Point2D(15, 20), editLine.End);
        Assert.Equal(editLine.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());
    }

    [Fact]
    public void SaveActiveBlockEdit_ShouldUpdateDefinitionAndRestoreReference()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        BlockDefinitionId definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginEditSelectedBlock();

        LineEntity editLine = Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.All.Single());
        var modifiedLine = new LineEntity(
            editLine.Start,
            new Point2D(18, 20),
            editLine.Id,
            editLine.LayerId,
            editLine.Style,
            editLine.IsVisible,
            editLine.IsLocked,
            editLine.DrawOrder);
        viewModel.Workspace.Document.ReplaceEntity(modifiedLine);
        viewModel.Workspace.SelectionSet.ReplaceWith(modifiedLine.Id);

        var result = viewModel.SaveActiveBlockEdit();

        Assert.True(result.Changed);
        Assert.False(viewModel.IsBlockEditSessionActive);
        BlockReferenceEntity restoredReference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(originalReference.Id, restoredReference.Id);
        Assert.Equal(restoredReference.Id, viewModel.Workspace.SelectionSet.SelectedIds.Single());

        LineEntity localLine = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.BlockDefinitions.GetRequired(definitionId).Entities.Single());
        Assert.Equal(Point2D.Origin, localLine.Start);
        Assert.Equal(new Point2D(8, 0), localLine.End);
    }

    [Fact]
    public void CancelActiveBlockEdit_ShouldRestoreReferenceWithoutChangingDefinition()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        BlockDefinitionId definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginEditSelectedBlock();

        var result = viewModel.CancelActiveBlockEdit();

        Assert.True(result.Changed);
        Assert.False(viewModel.IsBlockEditSessionActive);
        BlockReferenceEntity restoredReference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(originalReference.Id, restoredReference.Id);

        LineEntity localLine = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.BlockDefinitions.GetRequired(definitionId).Entities.Single());
        Assert.Equal(Point2D.Origin, localLine.Start);
        Assert.Equal(new Point2D(5, 0), localLine.End);
    }


    [Fact]
    public void SaveActiveBlockEdit_ShouldIgnorePreExistingExternalSelection()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        BlockDefinitionId definitionId = originalReference.BlockDefinitionId;
        var externalLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(110, 100));
        viewModel.Workspace.Document.AddEntity(externalLine);
        viewModel.Workspace.SelectionSet.ReplaceWith(originalReference.Id);
        viewModel.BeginEditSelectedBlock();
        viewModel.Workspace.SelectionSet.ReplaceWith(externalLine.Id);

        var result = viewModel.SaveActiveBlockEdit();

        Assert.True(result.Changed);
        Assert.False(viewModel.IsBlockEditSessionActive);
        Assert.Contains(viewModel.Workspace.Document.Entities.All, entity => entity.Id == externalLine.Id);
        Assert.Contains(viewModel.Workspace.Document.Entities.All, entity => entity.Id == originalReference.Id);
        Assert.Equal(2, viewModel.Workspace.Document.Entities.Count);

        LineEntity localLine = Assert.IsType<LineEntity>(
            viewModel.Workspace.Document.BlockDefinitions.GetRequired(definitionId).Entities.Single());
        Assert.Equal(Point2D.Origin, localLine.Start);
        Assert.Equal(new Point2D(5, 0), localLine.End);
    }

    [Fact]
    public void SaveActiveBlockEdit_ShouldIncludeEntitiesCreatedDuringSession()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        BlockDefinitionId definitionId = originalReference.BlockDefinitionId;
        viewModel.BeginEditSelectedBlock();
        var addedLine = new LineEntity(
            new Point2D(10, 21),
            new Point2D(15, 21));
        viewModel.Workspace.Document.AddEntity(addedLine);

        var result = viewModel.SaveActiveBlockEdit();

        Assert.True(result.Changed);
        Assert.False(viewModel.IsBlockEditSessionActive);
        Assert.Single(viewModel.Workspace.Document.Entities.All);

        IReadOnlyList<CadEntity> localEntities = viewModel.Workspace.Document.BlockDefinitions
            .GetRequired(definitionId)
            .Entities;
        Assert.Equal(2, localEntities.Count);
        Assert.Contains(localEntities.OfType<LineEntity>(), line => line.Start == Point2D.Origin && line.End == new Point2D(5, 0));
        Assert.Contains(localEntities.OfType<LineEntity>(), line => line.Start == new Point2D(0, 1) && line.End == new Point2D(5, 1));
    }

    [Fact]
    public void CancelActiveBlockEdit_ShouldRemoveEntitiesCreatedDuringSession()
    {
        var viewModel = CreateViewModelWithDoorBlock(out BlockReferenceEntity originalReference);
        viewModel.BeginEditSelectedBlock();
        var addedLine = new LineEntity(
            new Point2D(10, 21),
            new Point2D(15, 21));
        viewModel.Workspace.Document.AddEntity(addedLine);

        var result = viewModel.CancelActiveBlockEdit();

        Assert.True(result.Changed);
        Assert.False(viewModel.IsBlockEditSessionActive);
        BlockReferenceEntity restoredReference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());
        Assert.Equal(originalReference.Id, restoredReference.Id);
        Assert.DoesNotContain(viewModel.Workspace.Document.Entities.All, entity => entity.Id == addedLine.Id);
    }

    private static MainWindowViewModel CreateViewModelWithDoorBlock(
        out BlockReferenceEntity originalReference)
    {
        var viewModel = new MainWindowViewModel();
        var line = new LineEntity(
            new Point2D(10, 20),
            new Point2D(15, 20));
        viewModel.Workspace.Document.AddEntity(line);
        viewModel.Workspace.SelectionSet.ReplaceWith(line.Id);
        viewModel.CreateBlockFromSelection(new CreateBlockOptions("Door", 10, 20));

        originalReference = Assert.IsType<BlockReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        return viewModel;
    }
}
