using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class LineFormatCollectionTests
{
    [Fact]
    public void Default_ShouldContainBuiltInFormats()
    {
        LineFormatCollection formats = LineFormatCollection.Default;

        Assert.True(formats.Contains(LineFormatId.Continuous));
        Assert.True(formats.Contains(LineFormatId.Dashed));
        Assert.True(formats.Contains(LineFormatId.DashDot));
        Assert.True(formats.Contains(LineFormatId.DashDotDot));
        Assert.True(formats.Contains(LineFormatId.Axis));
        Assert.Equal(
            1.0,
            formats.GetById(LineFormatId.Continuous).LineWeight.Millimeters);
        Assert.Equal(
            0.5,
            formats.GetById(LineFormatId.Axis).LineWeight.Millimeters);
        Assert.Equal(
            1.0,
            formats.GetById(LineFormatId.Dashed).LineWeight.Millimeters);
        Assert.Equal(
            0.5,
            formats.GetById(LineFormatId.DashDotDot).LineWeight.Millimeters);
        Assert.Equal(
            0.75,
            formats.GetById(LineFormatId.DashDot).LineWeight.Millimeters);
        Assert.Equal(
            LineStyle.DashDot,
            formats.GetById(LineFormatId.Axis).LineStyle);
        Assert.Equal(
            CadColor.FromRgb(0, 255, 0),
            formats.GetById(LineFormatId.DashDot).Color);
        Assert.Equal(
            "Tratto due punti",
            formats.GetById(LineFormatId.DashDotDot).Name);
    }

    [Fact]
    public void Constructor_WithEmptyList_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new LineFormatCollection(Array.Empty<LineFormat>()));
    }

    [Fact]
    public void Constructor_WithDuplicateId_ShouldThrow()
    {
        LineFormat first = CreateFormat("A", "First");
        LineFormat second = CreateFormat("A", "Second");

        Assert.Throws<InvalidOperationException>(() =>
            new LineFormatCollection(new[] { first, second }));
    }

    [Fact]
    public void Constructor_WithDuplicateName_ShouldThrow()
    {
        LineFormat first = CreateFormat("A", "Same");
        LineFormat second = CreateFormat("B", "same");

        Assert.Throws<InvalidOperationException>(() =>
            new LineFormatCollection(new[] { first, second }));
    }

    [Fact]
    public void GetById_WithExistingId_ShouldReturnFormat()
    {
        LineFormat format = CreateFormat("A", "Custom");
        var collection = new LineFormatCollection(new[] { format });

        LineFormat result = collection.GetById(new LineFormatId("A"));

        Assert.Equal("Custom", result.Name);
    }

    [Fact]
    public void GetById_WithMissingId_ShouldThrow()
    {
        var collection = new LineFormatCollection(new[] { CreateFormat("A", "Custom") });

        Assert.Throws<KeyNotFoundException>(() =>
            collection.GetById(new LineFormatId("Missing")));
    }

    [Fact]
    public void TryGetById_WithMissingId_ShouldReturnFalse()
    {
        var collection = new LineFormatCollection(new[] { CreateFormat("A", "Custom") });

        bool found = collection.TryGetById(
            new LineFormatId("Missing"),
            out LineFormat? result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void WithFormats_ShouldReturnNewCollection()
    {
        var collection = new LineFormatCollection(new[] { CreateFormat("A", "First") });

        LineFormatCollection changed = collection.WithFormats(new[] { CreateFormat("B", "Second") });

        Assert.False(collection.Contains(new LineFormatId("B")));
        Assert.True(changed.Contains(new LineFormatId("B")));
    }

    [Fact]
    public void ReplaceAll_ShouldReplaceFormats()
    {
        var collection = new LineFormatCollection(new[] { CreateFormat("A", "First") });

        collection.ReplaceAll(new[] { CreateFormat("B", "Second") });

        Assert.False(collection.Contains(new LineFormatId("A")));
        Assert.True(collection.Contains(new LineFormatId("B")));
    }

    private static LineFormat CreateFormat(string id, string name)
    {
        return new LineFormat(
            new LineFormatId(id),
            name,
            CadColor.FromRgb(255, 255, 255),
            LineWeight.FromMillimeters(0.25),
            LineStyle.Continuous);
    }
}
