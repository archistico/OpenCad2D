using OpenCad2D.App.ViewModels;

namespace OpenCad2D.App.Tests;

public sealed class CommandHudFieldRoutingPolicyTests
{
    [Theory]
    [InlineData(null, false, CommandHudFieldKind.Distance)]
    [InlineData(null, true, CommandHudFieldKind.Y)]
    [InlineData(CommandHudFieldKind.Distance, false, CommandHudFieldKind.Angle)]
    [InlineData(CommandHudFieldKind.Angle, true, CommandHudFieldKind.Distance)]
    [InlineData(CommandHudFieldKind.Distance, true, CommandHudFieldKind.Y)]
    [InlineData(CommandHudFieldKind.Y, false, CommandHudFieldKind.Distance)]
    public void GetAdjacentFieldKind_WithEditableFields_ShouldNavigateAndWrap(
        CommandHudFieldKind? active, bool backwards, CommandHudFieldKind expected)
    {
        Assert.Equal(expected, CommandHudFieldRoutingPolicy.GetAdjacentFieldKind(
            new[] { CommandHudFieldKind.Distance, CommandHudFieldKind.Angle, CommandHudFieldKind.X, CommandHudFieldKind.Y },
            active, backwards));
    }

    [Fact]
    public void GetAdjacentFieldKind_WithoutEditableFields_ShouldReturnNull()
    {
        Assert.Null(CommandHudFieldRoutingPolicy.GetAdjacentFieldKind(
            new[] { CommandHudFieldKind.Generic }, null));
    }

    [Fact]
    public void GetDefaultFieldKindForNumericText_ShouldKeepActiveFieldWhenStillAvailable()
    {
        CommandHudFieldKind? result = CommandHudFieldRoutingPolicy.GetDefaultFieldKindForNumericText(
            new[]
            {
                CommandHudFieldKind.Distance,
                CommandHudFieldKind.Angle
            },
            CommandHudFieldKind.Angle);

        Assert.Equal(CommandHudFieldKind.Angle, result);
    }

    [Fact]
    public void GetDefaultFieldKindForNumericText_ShouldUseAngleWhenAngleIsOnlyEditableField()
    {
        CommandHudFieldKind? result = CommandHudFieldRoutingPolicy.GetDefaultFieldKindForNumericText(
            new[] { CommandHudFieldKind.Angle },
            activeKind: null);

        Assert.Equal(CommandHudFieldKind.Angle, result);
    }

    [Fact]
    public void GetDefaultFieldKindForNumericText_ShouldUseSidesWhenSidesIsOnlyEditableField()
    {
        CommandHudFieldKind? result = CommandHudFieldRoutingPolicy.GetDefaultFieldKindForNumericText(
            new[] { CommandHudFieldKind.Sides },
            activeKind: null);

        Assert.Equal(CommandHudFieldKind.Sides, result);
    }

    [Fact]
    public void GetDefaultFieldKindForNumericText_ShouldUseXForCoordinateOnlyHud()
    {
        CommandHudFieldKind? result = CommandHudFieldRoutingPolicy.GetDefaultFieldKindForNumericText(
            new[]
            {
                CommandHudFieldKind.X,
                CommandHudFieldKind.Y
            },
            activeKind: null);

        Assert.Equal(CommandHudFieldKind.X, result);
    }


    [Fact]
    public void GetDefaultFieldKindForNumericText_ShouldUseDistanceBeforeCoordinatesForSecondBreakPoint()
    {
        CommandHudFieldKind? result = CommandHudFieldRoutingPolicy.GetDefaultFieldKindForNumericText(
            new[]
            {
                CommandHudFieldKind.Distance,
                CommandHudFieldKind.Angle,
                CommandHudFieldKind.X,
                CommandHudFieldKind.Y
            },
            activeKind: null);

        Assert.Equal(CommandHudFieldKind.Distance, result);
    }

    [Fact]
    public void GetDefaultFieldKindForNumericText_ShouldKeepYWhenCoordinateHudTabsToY()
    {
        CommandHudFieldKind? result = CommandHudFieldRoutingPolicy.GetDefaultFieldKindForNumericText(
            new[]
            {
                CommandHudFieldKind.X,
                CommandHudFieldKind.Y
            },
            CommandHudFieldKind.Y);

        Assert.Equal(CommandHudFieldKind.Y, result);
    }

    [Theory]
    [InlineData("10", true)]
    [InlineData("-10.5", true)]
    [InlineData("10,5", true)]
    [InlineData("Y", false)]
    [InlineData("10<45", false)]
    public void IsNumericHudText_ShouldAcceptOnlyScalarNumericInput(
        string text,
        bool expected)
    {
        Assert.Equal(expected, CommandHudFieldRoutingPolicy.IsNumericHudText(text));
    }

    [Fact]
    public void GetDefaultFieldKindForNumericText_WithGapAndCoordinates_ShouldPreferX()
    {
        CommandHudFieldKind? result = CommandHudFieldRoutingPolicy.GetDefaultFieldKindForNumericText(
            new[]
            {
                CommandHudFieldKind.Gap,
                CommandHudFieldKind.X,
                CommandHudFieldKind.Y
            },
            activeKind: null);

        Assert.Equal(CommandHudFieldKind.X, result);
    }

}
