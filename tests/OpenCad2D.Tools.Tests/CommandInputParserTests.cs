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
        Assert.Equal(CommandInputParseKind.Distance, result.Kind);
        Assert.Equal(5, result.Distance);
    }

    [Fact]
    public void Parse_WhenInputIsDecimalDistance_ShouldReturnDistance()
    {
        CommandInputParseResult result = _parser.Parse("5.5");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.Distance, result.Kind);
        Assert.Equal(5.5, result.Distance);
    }

    [Fact]
    public void Parse_WhenInputIsAbsolutePoint_ShouldReturnAbsolutePoint()
    {
        CommandInputParseResult result = _parser.Parse("100,50");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.AbsolutePoint, result.Kind);
        Assert.Equal(new Point2D(100, 50), result.Point);
    }

    [Fact]
    public void Parse_WhenInputIsAbsoluteDecimalPoint_ShouldReturnAbsolutePoint()
    {
        CommandInputParseResult result = _parser.Parse("100.5,50.25");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.AbsolutePoint, result.Kind);
        Assert.Equal(new Point2D(100.5, 50.25), result.Point);
    }


    [Fact]
    public void Parse_WhenAbsolutePointContainsWhitespace_ShouldReturnAbsolutePoint()
    {
        CommandInputParseResult result = _parser.Parse(" 100.5, 50.25 ");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.AbsolutePoint, result.Kind);
        Assert.Equal(new Point2D(100.5, 50.25), result.Point);
    }

    [Theory]
    [InlineData("10,")]
    [InlineData(",20")]
    [InlineData("10,20,30")]
    [InlineData("abc,20")]
    [InlineData("10,abc")]
    public void Parse_WhenAbsolutePointIsInvalid_ShouldReturnInvalid(string input)
    {
        CommandInputParseResult result = _parser.Parse(input);

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputParseKind.Invalid, result.Kind);
        Assert.Equal(
            "Invalid absolute coordinate format. Use x,y for example: 100,50.",
            result.ErrorMessage);
    }

    [Fact]
    public void Parse_WhenInputIsRelativePoint_ShouldReturnRelativePoint()
    {
        CommandInputParseResult result = _parser.Parse("@50,0");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.RelativePoint, result.Kind);
        Assert.Equal(new Vector2D(50, 0), result.Offset);
    }



    [Fact]
    public void Parse_WhenRelativePointContainsWhitespace_ShouldReturnRelativePoint()
    {
        CommandInputParseResult result = _parser.Parse(" @100.5, -25.25 ");

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.RelativePoint, result.Kind);
        Assert.Equal(new Vector2D(100.5, -25.25), result.Offset);
    }

    [Theory]
    [InlineData("@")]
    [InlineData("@10,")]
    [InlineData("@,20")]
    [InlineData("@10,20,30")]
    [InlineData("@abc,20")]
    [InlineData("@10,abc")]
    public void Parse_WhenRelativePointIsInvalid_ShouldReturnInvalid(string input)
    {
        CommandInputParseResult result = _parser.Parse(input);

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputParseKind.Invalid, result.Kind);
        Assert.Equal(
            "Invalid relative coordinate format. Use @x,y for example: @50,0.",
            result.ErrorMessage);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Parse_WhenDistanceIsNotPositive_ShouldReturnInvalid(string input)
    {
        CommandInputParseResult result = _parser.Parse(input);

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputParseKind.Invalid, result.Kind);
        Assert.Equal("Distance must be greater than zero.", result.ErrorMessage);
    }

    [Fact]
    public void Parse_WhenInputIsEmpty_ShouldReturnInvalid()
    {
        CommandInputParseResult result = _parser.Parse(" ");

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputParseKind.Invalid, result.Kind);
    }

    [Fact]
    public void Parse_WhenInputIsNotValid_ShouldReturnInvalid()
    {
        CommandInputParseResult result = _parser.Parse("abc");

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputParseKind.Invalid, result.Kind);
    }
    [Theory]
    [InlineData("100<45", 100, 45)]
    [InlineData("100 < 45", 100, 45)]
    [InlineData("10.5<22.5", 10.5, 22.5)]
    [InlineData("100<-90", 100, 270)]
    [InlineData("100<450", 100, 90)]
    public void Parse_WhenInputIsDistanceAngle_ShouldReturnDistanceAngle(
        string input,
        double expectedDistance,
        double expectedAngle)
    {
        CommandInputParseResult result = _parser.Parse(input);

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.DistanceAngle, result.Kind);
        Assert.Equal(expectedDistance, result.Distance);
        Assert.Equal(expectedAngle, result.AngleDegrees);
    }

    [Theory]
    [InlineData("0<45")]
    [InlineData("-1<45")]
    public void Parse_WhenDistanceAngleDistanceIsNotPositive_ShouldReturnInvalid(string input)
    {
        CommandInputParseResult result = _parser.Parse(input);

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputParseKind.Invalid, result.Kind);
        Assert.Equal("Distance must be greater than zero.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("100<")]
    [InlineData("<45")]
    [InlineData("100<45<90")]
    [InlineData("abc<45")]
    [InlineData("100<abc")]
    public void Parse_WhenDistanceAngleIsInvalid_ShouldReturnInvalid(string input)
    {
        CommandInputParseResult result = _parser.Parse(input);

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputParseKind.Invalid, result.Kind);
        Assert.Equal(
            "Invalid distance-angle format. Use distance<angle for example: 100<45.",
            result.ErrorMessage);
    }



    [Theory]
    [InlineData("@100<45", 100, 45)]
    [InlineData("@100<-90", 100, 270)]
    public void Parse_WhenInputIsRelativePolarDistanceAngle_ShouldReturnDistanceAngle(
        string input,
        double expectedDistance,
        double expectedAngle)
    {
        CommandInputParseResult result = _parser.Parse(input);

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputParseKind.DistanceAngle, result.Kind);
        Assert.Equal(expectedDistance, result.Distance);
        Assert.Equal(expectedAngle, result.AngleDegrees);
    }

    [Fact]
    public void ParseContextual_WhenPromptExpectsCommand_ShouldReturnCommandSubmission()
    {
        CommandInputSubmission result = _parser.Parse(
            "line",
            CommandPromptState.Idle);

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Command, result.Kind);
        Assert.Equal("LINE", result.CommandName);
    }

    [Fact]
    public void ParseContextual_WhenInputIsEmptyAndPromptAcceptsEnter_ShouldReturnConfirm()
    {
        var prompt = new CommandPromptState(
            "TRIM",
            "Select cutting edges",
            CommandInputKind.SelectionOrOption,
            options: new[] { new CommandOption("All", "A", "Use all selectable entities") },
            acceptsEmptyEnter: true);

        CommandInputSubmission result = _parser.Parse("", prompt);

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Confirm, result.Kind);
    }

    [Fact]
    public void ParseContextual_WhenInputMatchesOptionShortcut_ShouldReturnOptionKeyword()
    {
        var prompt = new CommandPromptState(
            "POLYLINE",
            "Specify next point",
            CommandInputKind.PointOrOption,
            options: new[]
            {
                new CommandOption("Close", "C", "Close polyline"),
                new CommandOption("Undo", "U", "Undo last vertex")
            });

        CommandInputSubmission result = _parser.Parse("c", prompt, new Point2D(10, 20));

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Option, result.Kind);
        Assert.Equal("Close", result.OptionKeyword);
    }

    [Fact]
    public void ParseContextual_WhenPromptExpectsPoint_ShouldReturnAbsolutePointSubmission()
    {
        var prompt = new CommandPromptState(
            "LINE",
            "Specify first point",
            CommandInputKind.Point);

        CommandInputSubmission result = _parser.Parse("100,50", prompt);

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Point, result.Kind);
        Assert.Equal(new Point2D(100, 50), result.Point);
    }

    [Fact]
    public void ParseContextual_WhenPromptExpectsPoint_ShouldResolveRelativePointFromReference()
    {
        var prompt = new CommandPromptState(
            "LINE",
            "Specify second point",
            CommandInputKind.Point);

        CommandInputSubmission result = _parser.Parse(
            "@25,-10",
            prompt,
            new Point2D(100, 50));

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Point, result.Kind);
        Assert.Equal(new Point2D(125, 40), result.Point);
        Assert.Equal(new Vector2D(25, -10), result.Offset);
    }

    [Fact]
    public void ParseContextual_WhenPromptExpectsPoint_ShouldResolvePolarPointFromReference()
    {
        var prompt = new CommandPromptState(
            "LINE",
            "Specify second point",
            CommandInputKind.Point);

        CommandInputSubmission result = _parser.Parse(
            "@100<0",
            prompt,
            new Point2D(10, 20));

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Point, result.Kind);
        Assert.Equal(new Point2D(110, 20), result.Point);
        Assert.Equal(100, result.Distance);
        Assert.Equal(0, result.AngleDegrees);
    }

    [Fact]
    public void ParseContextual_WhenRelativePointHasNoReference_ShouldReturnInvalid()
    {
        var prompt = new CommandPromptState(
            "LINE",
            "Specify first point",
            CommandInputKind.Point);

        CommandInputSubmission result = _parser.Parse("@100,0", prompt);

        Assert.False(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Invalid, result.Kind);
        Assert.Equal("Relative coordinate input requires a reference point.", result.ErrorMessage);
    }

    [Fact]
    public void ParseContextual_WhenPromptExpectsDistance_ShouldReturnDistanceSubmission()
    {
        var prompt = new CommandPromptState(
            "CIRCLE",
            "Specify radius",
            CommandInputKind.Distance);

        CommandInputSubmission result = _parser.Parse("12.5", prompt);

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Distance, result.Kind);
        Assert.Equal(12.5, result.Distance);
    }


    [Fact]
    public void ParseContextual_WhenPromptExpectsPointOrDistance_ShouldReturnDistanceSubmission()
    {
        var prompt = new CommandPromptState(
            "LINE",
            "Specify second point",
            CommandInputKind.PointOrDistance);

        CommandInputSubmission result = _parser.Parse(
            "5",
            prompt,
            new Point2D(0, 0));

        Assert.True(result.IsValid);
        Assert.Equal(CommandInputSubmissionKind.Distance, result.Kind);
        Assert.Equal(5, result.Distance);
    }

}
