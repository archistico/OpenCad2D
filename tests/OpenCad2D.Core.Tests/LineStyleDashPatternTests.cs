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
    [InlineData(LineStyle.Dashed, new[] { 8.0, 4.0 })]
    [InlineData(LineStyle.DashDot, new[] { 12.0, 4.0, 1.0, 4.0 })]
    [InlineData(LineStyle.DashDotDot, new[] { 12.0, 4.0, 1.0, 4.0, 1.0, 4.0 })]
    public void Get_ForNonContinuousStyle_ShouldReturnExpectedPattern(
        LineStyle style,
        double[] expected)
    {
        double[]? pattern = LineStyleDashPattern.Get(style);

        Assert.NotNull(pattern);
        Assert.Equal(expected, pattern);
    }

    [Fact]
    public void IsValid_WithPositiveDashGapPairs_ShouldReturnTrue()
    {
        Assert.True(LineStyleDashPattern.IsValid(new[] { 10.0, 5.0, 1.0, 5.0 }));
    }

    [Theory]
    [InlineData(new[] { 10.0 })]
    [InlineData(new[] { 10.0, 0.0 })]
    [InlineData(new[] { 10.0, -1.0 })]
    public void IsValid_WithInvalidPattern_ShouldReturnFalse(double[] pattern)
    {
        Assert.False(LineStyleDashPattern.IsValid(pattern));
    }
}
