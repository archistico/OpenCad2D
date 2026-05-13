using OpenCad2D.Core.Documents;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfImportResultTests
{
    [Fact]
    public void HasWarnings_ShouldBeTrueWhenLogContainsWarning()
    {
        var result = new DxfImportResult(
            new CadDocument(),
            [new DxfImportLogEntry(DxfImportLogSeverity.Warning, "Unsupported entity skipped.")]);

        Assert.True(result.HasWarnings);
        Assert.False(result.HasErrors);
        Assert.Equal(1, result.Statistics.WarningCount);
        Assert.Equal(0, result.Statistics.ErrorCount);
    }

    [Fact]
    public void HasErrors_ShouldBeTrueWhenLogContainsError()
    {
        var result = new DxfImportResult(
            new CadDocument(),
            [new DxfImportLogEntry(DxfImportLogSeverity.Error, "Invalid file.")]);

        Assert.True(result.HasErrors);
        Assert.Equal(1, result.Statistics.ErrorCount);
    }
}
