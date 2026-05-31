using Avalonia.Input;
using OpenCad2D.App.Controls;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using Xunit;

namespace OpenCad2D.App.Tests;

public sealed class CadCanvasWorkspaceChangedEventArgsTests
{
    [Fact]
    public void Constructor_ShouldPreserveKeyModifiers()
    {
        var args = new CadCanvasWorkspaceChangedEventArgs(
            ToolResult.Updated("Selection toggled."),
            Point2D.Origin,
            keyModifiers: KeyModifiers.Shift);

        Assert.True(args.KeyModifiers.HasFlag(KeyModifiers.Shift));
    }
}
