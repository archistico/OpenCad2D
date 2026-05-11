using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentLineFormatTests
{
    [Fact]
    public void Constructor_ShouldCreateDefaultLineFormats()
    {
        var document = new CadDocument();

        Assert.True(document.LineFormats.Contains(LineFormatId.Continuous));
        Assert.True(document.LineFormats.Contains(LineFormatId.Axis));
    }

    [Fact]
    public void ReplaceLineFormats_ShouldReplaceDocumentFormats()
    {
        var document = new CadDocument();

        var formats = new LineFormatCollection(new[]
        {
            new LineFormat(
                LineFormatId.Continuous,
                "Custom continuous",
                CadColor.FromRgb(1, 2, 3),
                LineWeight.FromMillimeters(0.4),
                LineStyle.Continuous),
        });

        document.ReplaceLineFormats(formats);

        LineFormat result = document.LineFormats.GetById(LineFormatId.Continuous);
        Assert.Equal("Custom continuous", result.Name);
        Assert.Equal(CadColor.FromRgb(1, 2, 3), result.Color);
    }
}
