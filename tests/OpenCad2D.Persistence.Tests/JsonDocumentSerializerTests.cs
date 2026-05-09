using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class JsonDocumentSerializerTests
{
    [Fact]
    public void Serialize_WithLineEntity_ShouldCreateLineDto()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 4));

        document.AddEntity(line);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        Assert.Equal(JsonDocumentSerializer.CurrentVersion, dto.Version);
        Assert.Equal(LayerId.Default.Value, dto.Settings.CurrentLayerId);

        LineEntityDto lineDto = Assert.IsType<LineEntityDto>(Assert.Single(dto.Entities));

        Assert.Equal(line.Id.ToString(), lineDto.Id);
        Assert.Equal(1, lineDto.StartX);
        Assert.Equal(2, lineDto.StartY);
        Assert.Equal(3, lineDto.EndX);
        Assert.Equal(4, lineDto.EndY);
    }

    [Fact]
    public void Serialize_WithCircleEntity_ShouldCreateCircleDto()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(10, 20),
            30);

        document.AddEntity(circle);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CircleEntityDto circleDto = Assert.IsType<CircleEntityDto>(Assert.Single(dto.Entities));

        Assert.Equal(circle.Id.ToString(), circleDto.Id);
        Assert.Equal(10, circleDto.CenterX);
        Assert.Equal(20, circleDto.CenterY);
        Assert.Equal(30, circleDto.Radius);
    }

    [Fact]
    public void Serialize_ShouldIncludeViewportState()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto
            {
                PanX = 12.5,
                PanY = -4.25,
                Zoom = 3.5
            });

        Assert.Equal(12.5, dto.Viewport.PanX);
        Assert.Equal(-4.25, dto.Viewport.PanY);
        Assert.Equal(3.5, dto.Viewport.Zoom);
    }

    [Fact]
    public void Deserialize_ShouldRestoreLayers()
    {
        var serializer = new JsonDocumentSerializer();

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = "Walls"
            },
            Layers =
            {
                new LayerDto
                {
                    Id = "0",
                    Name = "0",
                    Color = "#FFFFFF",
                    LineWeight = 0.25,
                    IsVisible = true,
                    IsLocked = false
                },
                new LayerDto
                {
                    Id = "Walls",
                    Name = "Walls",
                    Color = "#112233",
                    LineWeight = 0.50,
                    IsVisible = false,
                    IsLocked = true
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out string currentLayerId,
            out _);

        Layer layer = document.Layers.GetRequired(new LayerId("Walls"));

        Assert.Equal("Walls", currentLayerId);
        Assert.Equal("Walls", layer.Name);
        Assert.Equal(0x11, layer.Color.R);
        Assert.Equal(0x22, layer.Color.G);
        Assert.Equal(0x33, layer.Color.B);
        Assert.Equal(0.50, layer.LineWeight.Millimeters);
        Assert.False(layer.IsVisible);
        Assert.True(layer.IsLocked);
    }

    [Fact]
    public void Deserialize_WithUnknownVersion_ShouldThrow()
    {
        var serializer = new JsonDocumentSerializer();

        var dto = new DocumentDto
        {
            Version = 999
        };

        Assert.Throws<UnsupportedDocumentVersionException>(() =>
            serializer.Deserialize(
                dto,
                out _,
                out _));
    }

    [Fact]
    public void Deserialize_WithUnknownEntityType_ShouldSkipEntity()
    {
        string json = """
        {
          "version": 1,
          "savedAt": "2026-05-09T12:00:00Z",
          "settings": { "currentLayerId": "0" },
          "viewport": { "panX": 0, "panY": 0, "zoom": 1 },
          "layers": [
            {
              "id": "0",
              "name": "0",
              "color": "#FFFFFF",
              "lineWeight": 0.25,
              "isVisible": true,
              "isLocked": false
            }
          ],
          "entities": [
            {
              "type": "FutureSpline",
              "id": "00000000-0000-0000-0000-000000000001",
              "layerId": "0"
            }
          ]
        }
        """;

        DocumentDto dto = JsonDocumentSerializer.FromJson(json);
        var serializer = new JsonDocumentSerializer();

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        Assert.Empty(document.Entities.All);
    }

    [Fact]
    public void SaveToFile_ThenLoadFromFile_ShouldRoundTripDto()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        document.AddEntity(
            new CircleEntity(
                new Point2D(5, 6),
                7));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto
            {
                PanX = 1,
                PanY = 2,
                Zoom = 3
            });

        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-test-{Guid.NewGuid():N}.opencad2d.json");

        try
        {
            serializer.SaveToFile(dto, filePath);

            DocumentDto loaded = serializer.LoadFromFile(filePath);

            Assert.Equal(JsonDocumentSerializer.CurrentVersion, loaded.Version);
            Assert.Single(loaded.Entities);
            Assert.Equal(3, loaded.Viewport.Zoom);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
