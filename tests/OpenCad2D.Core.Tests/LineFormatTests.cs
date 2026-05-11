using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class LineFormatTests
{
    [Fact]
    public void Constructor_WithEmptyId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new LineFormat(
                new LineFormatId(""),
                "Format",
                CadColor.FromRgb(255, 255, 255),
                LineWeight.FromMillimeters(0.25),
                LineStyle.Continuous));
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new LineFormat(
                new LineFormatId("Custom"),
                " ",
                CadColor.FromRgb(255, 255, 255),
                LineWeight.FromMillimeters(0.25),
                LineStyle.Continuous));
    }

    [Fact]
    public void IsBuiltIn_ShouldReturnTrue_ForBuiltInIds()
    {
        var format = new LineFormat(
            LineFormatId.Axis,
            "Asse",
            CadColor.FromRgb(255, 0, 0),
            LineWeight.FromMillimeters(0.13),
            LineStyle.DashDot);

        Assert.True(format.IsBuiltIn);
    }

    [Fact]
    public void IsBuiltIn_ShouldReturnFalse_ForUserDefinedId()
    {
        var format = new LineFormat(
            new LineFormatId("Custom"),
            "Custom",
            CadColor.FromRgb(255, 255, 255),
            LineWeight.FromMillimeters(0.25),
            LineStyle.Continuous);

        Assert.False(format.IsBuiltIn);
    }

    [Fact]
    public void WithName_ShouldKeepAppearanceAndChangeName()
    {
        var original = new LineFormat(
            new LineFormatId("Custom"),
            "Old",
            CadColor.FromRgb(1, 2, 3),
            LineWeight.FromMillimeters(0.35),
            LineStyle.Dashed);

        LineFormat changed = original.WithName("New");

        Assert.Equal(original.Id, changed.Id);
        Assert.Equal("New", changed.Name);
        Assert.Equal(original.Color, changed.Color);
        Assert.Equal(original.LineWeight, changed.LineWeight);
        Assert.Equal(original.LineStyle, changed.LineStyle);
    }

    [Fact]
    public void WithAppearance_ShouldKeepIdAndNameAndChangeAppearance()
    {
        var original = new LineFormat(
            new LineFormatId("Custom"),
            "Custom",
            CadColor.FromRgb(1, 2, 3),
            LineWeight.FromMillimeters(0.35),
            LineStyle.Dashed);

        LineFormat changed = original.WithAppearance(
            CadColor.FromRgb(10, 20, 30),
            LineWeight.FromMillimeters(0.5),
            LineStyle.DashDot);

        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.Name, changed.Name);
        Assert.Equal(CadColor.FromRgb(10, 20, 30), changed.Color);
        Assert.Equal(LineWeight.FromMillimeters(0.5), changed.LineWeight);
        Assert.Equal(LineStyle.DashDot, changed.LineStyle);
    }
}
