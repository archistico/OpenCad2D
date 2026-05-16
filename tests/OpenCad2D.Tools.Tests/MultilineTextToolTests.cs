using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.Tools.Tests;

public sealed class MultilineTextToolTests
{
    [Fact]
    public void OnPointerPressed_WhenInputIsProvided_ShouldCreateMultilineTextEntity()
    {
        var provider = new StubTextInputProvider(new TextInputResult(
            "First\nSecond",
            TextFormatId.Annotation,
            12));
        var tool = new MultilineTextTool(provider);
        ToolContext context = CreateContext();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        MultilineTextEntity text = Assert.Single(context.Document.Entities.All.OfType<MultilineTextEntity>());
        Assert.Equal(new Point2D(10, 20), text.InsertionPoint);
        Assert.Equal("First\nSecond", text.Text);
        Assert.Equal(TextFormatId.Annotation, text.TextFormatId);
        Assert.Equal(12, text.RotationDegrees, 6);
    }

    [Fact]
    public void OnPointerPressed_WhenInputIsCancelled_ShouldNotCreateEntity()
    {
        var tool = new MultilineTextTool(new StubTextInputProvider(null));
        ToolContext context = CreateContext();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Empty(context.Document.Entities.All);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
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
            Assert.True(request.IsMultiline);
            return _result;
        }
    }
}
