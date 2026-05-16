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
    public void Serialize_ShouldIncludeLineFormatsAndLayerReferences()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        LineFormatId customFormatId = new("CustomDashed");
        LayerId layerId = new("Walls");

        var customFormat = new LineFormat(
            customFormatId,
            "Custom dashed",
            CadColor.FromRgb(17, 34, 51),
            LineWeight.FromMillimeters(0.75),
            LineStyle.Dashed);

        document.ReplaceLineFormats(
            document.LineFormats.WithFormats(
                document.LineFormats.All.Concat(new[] { customFormat })));

        document.Layers.Add(
            new Layer(
                layerId,
                "Walls",
                customFormatId));

        DocumentDto dto = serializer.Serialize(
            document,
            layerId.Value,
            new ViewportStateDto());

        Assert.Contains(dto.LineFormats, format =>
            format.Id == customFormatId.Value &&
            format.Name == "Custom dashed" &&
            format.Color == "#112233" &&
            format.LineWeight == 0.75 &&
            format.LineStyle == nameof(LineStyle.Dashed));

        LayerDto layerDto = Assert.Single(
            dto.Layers,
            layer => layer.Id == layerId.Value);

        Assert.Equal(customFormatId.Value, layerDto.LineFormatId);
    }

    [Fact]
    public void Deserialize_ShouldRestoreLayersAndLineFormats()
    {
        var serializer = new JsonDocumentSerializer();

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = "Walls"
            },
            LineFormats =
            {
                new LineFormatDto
                {
                    Id = "Continuous",
                    Name = "Continua",
                    Color = "#FFFFFF",
                    LineWeight = 1.0,
                    LineStyle = "Continuous"
                },
                new LineFormatDto
                {
                    Id = "WallsFormat",
                    Name = "Walls format",
                    Color = "#112233",
                    LineWeight = 0.50,
                    LineStyle = "DashDot"
                }
            },
            Layers =
            {
                new LayerDto
                {
                    Id = "0",
                    Name = "0",
                    LineFormatId = "Continuous",
                    IsVisible = true,
                    IsLocked = false
                },
                new LayerDto
                {
                    Id = "Walls",
                    Name = "Walls",
                    LineFormatId = "WallsFormat",
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
        LineFormat format = document.LineFormats.GetById(new LineFormatId("WallsFormat"));

        Assert.Equal("Walls", currentLayerId);
        Assert.Equal("Walls", layer.Name);
        Assert.Equal(new LineFormatId("WallsFormat"), layer.LineFormatId);
        Assert.False(layer.IsVisible);
        Assert.True(layer.IsLocked);
        Assert.Equal(0x11, format.Color.R);
        Assert.Equal(0x22, format.Color.G);
        Assert.Equal(0x33, format.Color.B);
        Assert.Equal(0.50, format.LineWeight.Millimeters);
        Assert.Equal(LineStyle.DashDot, format.LineStyle);
    }

    [Fact]
    public void Deserialize_WithoutLineFormats_ShouldUseDefaults()
    {
        var serializer = new JsonDocumentSerializer();

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = "0"
            },
            Layers =
            {
                new LayerDto
                {
                    Id = "0",
                    Name = "0",
                    LineFormatId = "Continuous"
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        Assert.True(document.LineFormats.Contains(LineFormatId.Continuous));
        Assert.Equal(
            1.0,
            document.LineFormats.GetById(LineFormatId.Continuous).LineWeight.Millimeters);
    }

    [Fact]
    public void Deserialize_WithUnknownLayerLineFormat_ShouldFallbackToContinuous()
    {
        var serializer = new JsonDocumentSerializer();

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = "0"
            },
            LineFormats =
            {
                new LineFormatDto
                {
                    Id = "Continuous",
                    Name = "Continua",
                    Color = "#FFFFFF",
                    LineWeight = 1.0,
                    LineStyle = "Continuous"
                }
            },
            Layers =
            {
                new LayerDto
                {
                    Id = "0",
                    Name = "0",
                    LineFormatId = "MissingFormat"
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        Layer layer = document.Layers.GetRequired(LayerId.Default);

        Assert.Equal(LineFormatId.Continuous, layer.LineFormatId);
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
          "lineFormats": [
            {
              "id": "Continuous",
              "name": "Continua",
              "color": "#FFFFFF",
              "lineWeight": 1.0,
              "lineStyle": "Continuous"
            }
          ],
          "layers": [
            {
              "id": "0",
              "name": "0",
              "lineFormatId": "Continuous",
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
            Assert.NotEmpty(loaded.LineFormats);
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
    [Fact]
    public void DeserializeWithRecovery_WhenEntityLayerIsMissing_ShouldMoveEntityToDefaultLayer()
    {
        var serializer = new JsonDocumentSerializer();
        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = "MissingLayer"
            },
            Entities =
            {
                new LineEntityDto
                {
                    Id = Guid.NewGuid().ToString(),
                    LayerId = "MissingLayer",
                    StartX = 0,
                    StartY = 0,
                    EndX = 10,
                    EndY = 0
                }
            }
        };

        DocumentRecoveryResult result = serializer.DeserializeWithRecovery(dto);

        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(LayerId.Default, line.LayerId);
        Assert.Equal(LayerId.Default.Value, result.CurrentLayerId);
        Assert.Equal(1, result.RecoveredEntityCount);
        Assert.Equal(0, result.SkippedEntityCount);
        Assert.Contains(result.Issues, issue => issue.Code == "ENTITY_LAYER_REPAIRED");
        Assert.Contains(result.Issues, issue => issue.Code == "CURRENT_LAYER_REPAIRED");
    }

    [Fact]
    public void DeserializeWithRecovery_WhenEntityIdIsInvalid_ShouldSkipOnlyInvalidEntity()
    {
        var serializer = new JsonDocumentSerializer();
        var validId = Guid.NewGuid().ToString();
        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Entities =
            {
                new LineEntityDto
                {
                    Id = "not-a-guid",
                    LayerId = LayerId.Default.Value,
                    StartX = 0,
                    StartY = 0,
                    EndX = 10,
                    EndY = 0
                },
                new LineEntityDto
                {
                    Id = validId,
                    LayerId = LayerId.Default.Value,
                    StartX = 1,
                    StartY = 1,
                    EndX = 2,
                    EndY = 2
                }
            }
        };

        DocumentRecoveryResult result = serializer.DeserializeWithRecovery(dto);

        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(validId, line.Id.ToString());
        Assert.Equal(1, result.RecoveredEntityCount);
        Assert.Equal(1, result.SkippedEntityCount);
        Assert.Contains(result.Issues, issue => issue.Code == "ENTITY_SKIPPED");
    }

}
