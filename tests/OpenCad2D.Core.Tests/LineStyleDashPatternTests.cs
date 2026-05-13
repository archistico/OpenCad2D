using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class LineStyleDashPatternTests
{
    [Fact]
    public void Get_ForContinuous_ShouldReturnNull()
    {
        Assert.Null(LineStyleDashPattern.Get(LineStyle.Continuous));
    }

    [Theory]
    [InlineData(LineStyle.Dashed, new[] { 6.0, 3.0 })]
    [InlineData(LineStyle.DashDot, new[] { 12.0, 4.0, 2.0, 4.0 })]
    [InlineData(LineStyle.DashDotDot, new[] { 12.0, 4.0, 2.0, 4.0, 2.0, 4.0 })]
    public void Get_ForNonContinuousStyle_ShouldReturnExpectedPattern(
        LineStyle style,
        double[] expected)
    {
        double[]? pattern = LineStyleDashPattern.Get(style);

        Assert.NotNull(pattern);
        Assert.Equal(expected, pattern);
    }
}
