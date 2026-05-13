using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelDxfImportTests
{
    [Fact]
    public void ImportDxfFromFile_WithValidLine_ShouldReplaceDocumentAndMarkDirty()
    {
        string filePath = CreateTemporaryDxf("""
0
SECTION
2
ENTITIES
0
LINE
8
ImportedLayer
10
0
20
0
11
100
21
50
0
ENDSEC
0
EOF
""");

        try
        {
            var viewModel = new MainWindowViewModel();

            Assert.True(viewModel.EntityCount > 1);

            var result = viewModel.ImportDxfFromFile(filePath);

            Assert.False(result.HasErrors);
            Assert.Single(viewModel.Workspace.Document.Entities.All);
            Assert.IsType<LineEntity>(viewModel.Workspace.Document.Entities.All.Single());
            Assert.Contains(viewModel.LayerNames, name => name == "ImportedLayer");
            Assert.Null(viewModel.CurrentFilePath);
            Assert.Equal("Untitled", viewModel.CurrentFileName);
            Assert.True(viewModel.IsDirty);
            Assert.Contains("Imported DXF", viewModel.LastMessage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ImportDxfFromFile_WithMalformedDxf_ShouldKeepCurrentDocument()
    {
        string filePath = CreateTemporaryDxf("""
0
SECTION
2
ENTITIES
0
LINE
8
Layer1
10
0
20
""");

        try
        {
            var viewModel = new MainWindowViewModel();
            int originalEntityCount = viewModel.EntityCount;

            var result = viewModel.ImportDxfFromFile(filePath);

            Assert.True(result.HasErrors);
            Assert.Equal(originalEntityCount, viewModel.EntityCount);
            Assert.Contains("DXF import failed", viewModel.LastMessage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTemporaryDxf(string content)
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-import-{Guid.NewGuid():N}.dxf");

        File.WriteAllText(
            filePath,
            content.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));

        return filePath;
    }
}
