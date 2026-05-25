using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;
using System.Text.Json;

namespace OpenCad2D.Persistence.Tests;

public sealed class ImageReferenceRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveExternalImageReference()
    {
        var document = new CadDocument();
        var image = new ImageReferenceEntity(
            @"C:\Temp\plan.png",
            new Point2D(10, 20),
            new Vector2D(30, 0),
            new Vector2D(0, 15),
            pixelWidth: 1200,
            pixelHeight: 600,
            id: new EntityId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        document.AddEntity(image);

        var serializer = new JsonDocumentSerializer();
        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        ImageReferenceEntity restoredImage = Assert.IsType<ImageReferenceEntity>(
            restored.Entities.GetRequired(new EntityId(Guid.Parse("11111111-1111-1111-1111-111111111111"))));

        Assert.Equal(@"C:\Temp\plan.png", restoredImage.FilePath);
        Assert.Equal(image.Origin, restoredImage.Origin);
        Assert.Equal(image.WidthVector, restoredImage.WidthVector);
        Assert.Equal(image.HeightVector, restoredImage.HeightVector);
        Assert.Equal(1200, restoredImage.PixelWidth);
        Assert.Equal(600, restoredImage.PixelHeight);
    }

    [Fact]
    public void SaveToFile_ShouldStoreImagePathRelativeToDocumentFolder()
    {
        string tempRoot = Path.Combine(
            Path.GetTempPath(),
            "OpenCad2D.Persistence.Tests",
            Guid.NewGuid().ToString("N"));
        string documentFolder = Path.Combine(tempRoot, "drawings");
        string imageFolder = Path.Combine(documentFolder, "images");
        string imagePath = Path.Combine(imageFolder, "plan.png");
        string documentPath = Path.Combine(documentFolder, "drawing.opencad2d.json");

        try
        {
            Directory.CreateDirectory(imageFolder);
            File.WriteAllText(imagePath, string.Empty);

            var document = new CadDocument();
            document.AddEntity(new ImageReferenceEntity(
                imagePath,
                new Point2D(0, 0),
                new Vector2D(10, 0),
                new Vector2D(0, 5)));

            var serializer = new JsonDocumentSerializer();
            DocumentDto dto = serializer.Serialize(
                document,
                LayerId.Default.Value,
                new ViewportStateDto());

            serializer.SaveToFile(dto, documentPath);

            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(documentPath));
            string? savedPath = json.RootElement
                .GetProperty("entities")[0]
                .GetProperty("filePath")
                .GetString();

            Assert.Equal(Path.Combine("images", "plan.png"), savedPath);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void LoadFromFile_ShouldResolveRelativeImagePathAgainstDocumentFolder()
    {
        string tempRoot = Path.Combine(
            Path.GetTempPath(),
            "OpenCad2D.Persistence.Tests",
            Guid.NewGuid().ToString("N"));
        string documentFolder = Path.Combine(tempRoot, "drawings");
        string documentPath = Path.Combine(documentFolder, "drawing.opencad2d.json");
        string expectedImagePath = Path.GetFullPath(Path.Combine(documentFolder, "images", "plan.png"));

        try
        {
            Directory.CreateDirectory(documentFolder);

            var serializer = new JsonDocumentSerializer();
            var dto = new DocumentDto
            {
                Version = JsonDocumentSerializer.CurrentVersion,
                Settings = new DocumentSettingsDto
                {
                    CurrentLayerId = LayerId.Default.Value
                },
                Viewport = new ViewportStateDto(),
                Entities =
                {
                    new ImageReferenceEntityDto
                    {
                        Id = "11111111-1111-1111-1111-111111111111",
                        LayerId = LayerId.Default.Value,
                        FilePath = Path.Combine("images", "plan.png"),
                        WidthVectorX = 10,
                        WidthVectorY = 0,
                        HeightVectorX = 0,
                        HeightVectorY = 5
                    }
                }
            };

            File.WriteAllText(documentPath, JsonDocumentSerializer.ToJson(dto));

            DocumentDto loadedDto = serializer.LoadFromFile(documentPath);
            CadDocument document = serializer.Deserialize(
                loadedDto,
                out _,
                out _);

            ImageReferenceEntity image = Assert.IsType<ImageReferenceEntity>(
                document.Entities.GetRequired(new EntityId(Guid.Parse("11111111-1111-1111-1111-111111111111"))));

            Assert.Equal(expectedImagePath, image.FilePath);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

}
