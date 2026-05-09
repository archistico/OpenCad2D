using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Tests;

public sealed class CadWorkspaceLockedLayerTests
{
    [Fact]
    public void SetCurrentLayerLocked_WhenSelectedEntityIsOnCurrentLayer_ShouldClearSelection()
    {
        var workspace = new CadWorkspace();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        workspace.Document.AddEntity(line);
        workspace.SelectionSet.Select(line.Id);

        ToolResult result = workspace.SetCurrentLayerLocked(true);

        Assert.True(result.Changed);
        Assert.True(workspace.CurrentLayerId.Equals(LayerId.Default));
        Assert.True(workspace.Document.Layers.GetRequired(LayerId.Default).IsLocked);
        Assert.False(workspace.SelectionSet.Contains(line.Id));
        Assert.True(workspace.SelectionSet.IsEmpty);
    }

    [Fact]
    public void SetCurrentLayerLocked_WhenSelectedEntityIsOnDifferentUnlockedLayer_ShouldKeepSelection()
    {
        var workspace = new CadWorkspace();

        var otherLayerId = new LayerId("Other");

        workspace.Document.Layers.Add(
            new Layer(
                otherLayerId,
                "Other"));

        var lineOnOtherLayer = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: otherLayerId);

        workspace.Document.AddEntity(lineOnOtherLayer);
        workspace.SelectionSet.Select(lineOnOtherLayer.Id);

        workspace.SetCurrentLayerLocked(true);

        Assert.True(workspace.Document.Layers.GetRequired(LayerId.Default).IsLocked);
        Assert.True(workspace.SelectionSet.Contains(lineOnOtherLayer.Id));
    }

    [Fact]
    public void ClearSelectionOfNonSelectableEntities_WhenEntityIsOnLockedLayer_ShouldDeselectEntity()
    {
        var workspace = new CadWorkspace();

        var layerId = new LayerId("Reference");

        workspace.Document.Layers.Add(
            new Layer(
                layerId,
                "Reference",
                isLocked: true));

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId);

        workspace.Document.AddEntity(line);
        workspace.SelectionSet.Select(line.Id);

        int removed = workspace.ClearSelectionOfNonSelectableEntities();

        Assert.Equal(1, removed);
        Assert.False(workspace.SelectionSet.Contains(line.Id));
    }
}