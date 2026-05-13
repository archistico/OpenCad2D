using System;
using System.IO;
using OpenCad2D.App.ViewModels;
using OpenCad2D.Export.Pdf;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelPdfExportTests
{
    [Fact]
    public void ExportPdfToFile_ShouldCreatePdfFileAndUpdateMessage()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-{Guid.NewGuid():N}.pdf");

        try
        {
            var viewModel = new MainWindowViewModel();

            var result = viewModel.ExportPdfToFile(filePath);

            Assert.True(File.Exists(filePath));
            byte[] content = File.ReadAllBytes(filePath);
            Assert.True(content.Length > 0);
            Assert.StartsWith("%PDF-1.4", System.Text.Encoding.ASCII.GetString(content, 0, 8));
            Assert.Equal(result.Content, content);
            Assert.Contains("Exported PDF", viewModel.LastMessage);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }


    [Fact]
    public void ExportPdfToFile_WithCustomOptions_ShouldCreatePdfFileAndUpdateMessage()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-{Guid.NewGuid():N}.pdf");

        try
        {
            var viewModel = new MainWindowViewModel();
            var options = new PdfExportOptions
            {
                PageSize = PdfPageSize.A3,
                Orientation = PdfPageOrientation.Landscape,
                MarginMillimeters = 15,
                IncludeHiddenLayers = true,
                UsePrintFriendlyColors = false
            };

            var result = viewModel.ExportPdfToFile(
                filePath,
                options);

            Assert.True(File.Exists(filePath));
            Assert.NotEmpty(result.Content);
            Assert.Contains("A3 Landscape", viewModel.LastMessage);
            Assert.Contains("margin 15 mm", viewModel.LastMessage);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ExportPdfToFile_WithEmptyPath_ShouldThrow()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Throws<ArgumentException>(() => viewModel.ExportPdfToFile(""));
    }
}
