using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class CommandAliasRegistryTests
{
    [Theory]
    [InlineData("L", ToolId.Line)]
    [InlineData("line", ToolId.Line)]
    [InlineData("C", ToolId.Circle)]
    [InlineData("TR", ToolId.Trim)]
    [InlineData("EX", ToolId.Extend)]
    [InlineData("HDIM", ToolId.HorizontalDimension)]
    [InlineData("ANG", ToolId.AngularDimension)]
    [InlineData("  pl  ", ToolId.Polyline)]
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
