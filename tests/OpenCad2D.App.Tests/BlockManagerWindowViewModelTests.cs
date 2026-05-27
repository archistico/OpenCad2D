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
    public void Constructor_ShouldListDefinitionsWithInstanceCounts()
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
        Assert.Equal(1, viewModel.Blocks.Single(block => block.Id == used.Id).InstanceCount);
        Assert.Equal(0, viewModel.Blocks.Single(block => block.Id == unused.Id).InstanceCount);
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
