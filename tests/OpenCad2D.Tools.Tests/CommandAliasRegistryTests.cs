using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class CommandAliasRegistryTests
{
    [Theory]
    [InlineData("L", ToolId.Line)]
    [InlineData("line", ToolId.Line)]
    [InlineData("MTEXT", ToolId.MultilineText)]
    [InlineData("mt", ToolId.MultilineText)]
    [InlineData("C", ToolId.Circle)]
    [InlineData("EL", ToolId.Ellipse)]
    [InlineData("ellipse", ToolId.Ellipse)]
    [InlineData("TR", ToolId.Trim)]
    [InlineData("O", ToolId.Offset)]
    [InlineData("BFILL", ToolId.BoundaryFill)]
    [InlineData("bf", ToolId.BoundaryFill)]
    [InlineData("FILL", ToolId.BoundaryFill)]
    [InlineData("RIEMPIMENTO", ToolId.BoundaryFill)]
    [InlineData("F", ToolId.Fillet)]
    [InlineData("CHA", ToolId.Chamfer)]
    [InlineData("chamfer", ToolId.Chamfer)]
    [InlineData("MI", ToolId.Mirror)]
    [InlineData("mirror", ToolId.Mirror)]
    [InlineData("EXPLODE", ToolId.Explode)]
    [InlineData("x", ToolId.Explode)]
    [InlineData("JOIN", ToolId.Join)]
    [InlineData("j", ToolId.Join)]
    [InlineData("ZW", ToolId.ZoomWindow)]
    [InlineData("EX", ToolId.Extend)]
    [InlineData("HDIM", ToolId.HorizontalDimension)]
    [InlineData("ANG", ToolId.AngularDimension)]
    [InlineData("  pl  ", ToolId.Polyline)]
    [InlineData("SPLINE", ToolId.Spline)]
    [InlineData("spl", ToolId.Spline)]
    [InlineData("PG", ToolId.Polygon)]
    [InlineData("polygon", ToolId.Polygon)]
    [InlineData("NORTH", ToolId.NorthSymbol)]
    [InlineData("ns", ToolId.NorthSymbol)]
    [InlineData("SCALEBAR", ToolId.ScaleBar)]
    [InlineData("sbar", ToolId.ScaleBar)]
    [InlineData("GRAPHICSCALE", ToolId.ScaleBar)]
    public void TryResolve_WithKnownAlias_ShouldReturnTool(
        string alias,
        ToolId expectedToolId)
    {
        CommandAliasRegistry registry = CommandAliasRegistry.CreateDefault();

        bool resolved = registry.TryResolve(
            alias,
            out ToolId actualToolId);

        Assert.True(resolved);
        Assert.Equal(expectedToolId, actualToolId);
    }

    [Fact]
    public void TryResolve_WithUnknownAlias_ShouldReturnFalse()
    {
        CommandAliasRegistry registry = CommandAliasRegistry.CreateDefault();

        bool resolved = registry.TryResolve(
            "UNKNOWN",
            out _);

        Assert.False(resolved);
    }

    [Fact]
    public void Constructor_WithDuplicateAlias_ShouldThrow()
    {
        var aliases = new[]
        {
            new CommandAlias("L", ToolId.Line),
            new CommandAlias("l", ToolId.Polyline)
        };

        Assert.Throws<InvalidOperationException>(() => new CommandAliasRegistry(aliases));
    }

    [Fact]
    public void Constructor_WithEmptyAlias_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new CommandAlias(" ", ToolId.Line));
    }
}
