using System;
using System.IO;
using OpenCad2D.App.ViewModels;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelDxfExportTests
{
    [Fact]
    public void ExportDxfToFile_ShouldCreateDxfFileAndUpdateMessage()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-{Guid.NewGuid():N}.dxf");

        try
        {
            var viewModel = new MainWindowViewModel();

            var result = viewModel.ExportDxfToFile(filePath);

            Assert.True(File.Exists(filePath));
            Assert.Contains("0\r\nSECTION", File.ReadAllText(filePath));
            Assert.Equal(result.Content, File.ReadAllText(filePath));
            Assert.Contains("Exported DXF", viewModel.LastMessage);
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
    public void ExportDxfToFile_WithEmptyPath_ShouldThrow()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Throws<ArgumentException>(() => viewModel.ExportDxfToFile(""));
    }
}
