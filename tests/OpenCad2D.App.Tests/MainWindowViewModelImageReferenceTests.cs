using OpenCad2D.App.ViewModels;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class MainWindowViewModelImageReferenceTests
{
    [Fact]
    public void ReplaceSelectedImageReference_ShouldRelinkSelectedImageAndKeepGeometry()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.AddImageReference(
            "old.png",
            Point2D.Origin,
            width: 10,
            height: 5,
            pixelWidth: 100,
            pixelHeight: 50);

        Assert.True(viewModel.HasSingleSelectedImageReference);

        viewModel.ReplaceSelectedImageReference(
            "new.jpg",
            pixelWidth: 400,
            pixelHeight: 200);

        ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
            viewModel.Workspace.Document.Entities.All.Single());

        Assert.Equal("new.jpg", image.FilePath);
        Assert.Equal(400, image.PixelWidth);
        Assert.Equal(200, image.PixelHeight);
        Assert.Equal(10, image.Width, precision: 6);
        Assert.Equal(5, image.Height, precision: 6);
        Assert.True(viewModel.HasSingleSelectedImageReference);
    }
}
