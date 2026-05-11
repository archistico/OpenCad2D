using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class SelectionToolSnapModeTests
{
    [Fact]
    public void GetActiveSnapKind_ShouldUseEntitySnapOnly()
    {
        var context = new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: SnapKind.Endpoint | SnapKind.Midpoint | SnapKind.Grid,
            snapTolerance: 5);

        var tool = new SelectionTool();

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
    }
}
