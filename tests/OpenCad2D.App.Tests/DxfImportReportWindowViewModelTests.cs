using OpenCad2D.App.ViewModels.DxfImport;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class DxfImportReportWindowViewModelTests
{
    [Fact]
    public void Constructor_WithSuccessfulImport_ShouldExposeSummaryAndEntityRows()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0)));
        document.AddEntity(new CircleEntity(
            new Point2D(5, 5),
            3));

        var result = new DxfImportResult(
            document,
            Array.Empty<DxfImportLogEntry>());

        var viewModel = new DxfImportReportWindowViewModel(result);

        Assert.Equal("DXF imported successfully", viewModel.StatusTitle);
        Assert.Equal(2, viewModel.TotalImportedEntities);
        Assert.Equal(1, viewModel.ImportedLayerCount);
        Assert.Equal(0, viewModel.WarningCount);
        Assert.Equal(0, viewModel.ErrorCount);
        Assert.True(viewModel.HasEntityRows);
        Assert.False(viewModel.HasDiagnosticRows);
        Assert.Contains(viewModel.EntityRows, row => row.EntityKind == "Line" && row.Count == 1);
        Assert.Contains(viewModel.EntityRows, row => row.EntityKind == "Circle" && row.Count == 1);
    }

    [Fact]
    public void Constructor_WithWarnings_ShouldExposeDiagnosticRows()
    {
        var document = new CadDocument();
        var result = new DxfImportResult(
            document,
            new[]
            {
                new DxfImportLogEntry(
                    DxfImportLogSeverity.Warning,
                    "Skipped unsupported DXF entity: HATCH.",
                    42)
            });

        var viewModel = new DxfImportReportWindowViewModel(result);

        Assert.Equal("DXF imported with warnings", viewModel.StatusTitle);
        Assert.Equal(1, viewModel.WarningCount);
        Assert.Equal(0, viewModel.ErrorCount);
        Assert.Equal(1, viewModel.SkippedRecordCount);
        Assert.True(viewModel.HasDiagnosticRows);

        DxfImportDiagnosticRowViewModel row = Assert.Single(viewModel.DiagnosticRows);
        Assert.Equal("Warning", row.Severity);
        Assert.Equal("42", row.Line);
        Assert.Equal("Skipped unsupported DXF entity: HATCH.", row.Message);
    }

    [Fact]
    public void Constructor_WithErrors_ShouldExposeFailureStatus()
    {
        var document = new CadDocument();
        var result = new DxfImportResult(
            document,
            new[]
            {
                new DxfImportLogEntry(
                    DxfImportLogSeverity.Error,
                    "Malformed DXF file.")
            });

        var viewModel = new DxfImportReportWindowViewModel(result);

        Assert.Equal("DXF import failed", viewModel.StatusTitle);
        Assert.Equal(0, viewModel.WarningCount);
        Assert.Equal(1, viewModel.ErrorCount);
        Assert.True(viewModel.HasDiagnosticRows);

        DxfImportDiagnosticRowViewModel row = Assert.Single(viewModel.DiagnosticRows);
        Assert.Equal("Error", row.Severity);
        Assert.Equal("-", row.Line);
        Assert.Equal("Malformed DXF file.", row.Message);
    }
}
