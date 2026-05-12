using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.Tools.Tests;

public sealed class TextToolTests
{
    [Fact]
    public void PointerPress_WithInput_ShouldCreateTextOnCurrentLayer()
    {
        LayerId layerId = new("Annotations");
        var document = new CadDocument();
        document.Layers.Add(new Layer(layerId, "Annotations"));
        ToolContext context = CreateContext(document, layerId);
        var provider = new StubTextInputProvider(new TextInputResult("Entrance", TextFormatId.Annotation, 30));
        var tool = new TextTool(provider);

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        TextEntity text = Assert.Single(context.Document.Entities.All.OfType<TextEntity>());
        Assert.Equal("Entrance", text.Text);
        Assert.Equal(new Point2D(10, 20), text.InsertionPoint);
        Assert.Equal(30, text.RotationDegrees);
        Assert.Equal(TextFormatId.Annotation, text.TextFormatId);
        Assert.Equal(layerId, text.LayerId);
        Assert.True(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void PointerPress_WhenInputCancelled_ShouldNotCreateEntity()
    {
        ToolContext context = CreateContext();
        var tool = new TextTool(new StubTextInputProvider(null));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Empty(context.Document.Entities.All);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        LayerId? currentLayerId = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            currentLayerId: currentLayerId);
    }

    private sealed class StubTextInputProvider : ITextInputProvider
    {
        private readonly TextInputResult? _result;

        public StubTextInputProvider(TextInputResult? result)
        {
            _result = result;
        }

        public TextInputResult? RequestText(TextInputRequest request)
        {
            return _result;
        }
    }
}
