using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class CommandInputParserTests
{
    private readonly CommandInputParser _parser = new();

    [Fact]
    public void Parse_WhenInputIsIntegerDistance_ShouldReturnDistance()
    {
        CommandInputParseResult result = _parser.Parse("5");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputKind.Distance, result.Kind);
        Assert.Equal(5, result.Distance);
    }

    [Fact]
    public void Parse_WhenInputIsDecimalDistance_ShouldReturnDistance()
    {
        CommandInputParseResult result = _parser.Parse("5.5");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputKind.Distance, result.Kind);
        Assert.Equal(5.5, result.Distance);
    }

    [Fact]
    public void Parse_WhenInputIsAbsolutePoint_ShouldReturnAbsolutePoint()
    {
        CommandInputParseResult result = _parser.Parse("100,50");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputKind.AbsolutePoint, result.Kind);
        Assert.Equal(new Point2D(100, 50), result.Point);
    }

    [Fact]
    public void Parse_WhenInputIsAbsoluteDecimalPoint_ShouldReturnAbsolutePoint()
    {
        CommandInputParseResult result = _parser.Parse("100.5,50.25");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputKind.AbsolutePoint, result.Kind);
        Assert.Equal(new Point2D(100.5, 50.25), result.Point);
    }

    [Fact]
    public void Parse_WhenInputIsRelativePoint_ShouldReturnRelativePoint()
    {
        CommandInputParseResult result = _parser.Parse("@50,0");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputKind.RelativePoint, result.Kind);
        Assert.Equal(new Vector2D(50, 0), result.Offset);
    }

    [Fact]
    public void Parse_WhenInputIsEmpty_ShouldReturnInvalid()
    {
        CommandInputParseResult result = _parser.Parse(" ");

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputKind.Invalid, result.Kind);
    }

    [Fact]
    public void Parse_WhenInputIsNotValid_ShouldReturnInvalid()
    {
        CommandInputParseResult result = _parser.Parse("abc");

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputKind.Invalid, result.Kind);
    }
}
