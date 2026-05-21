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


    [Fact]
    public void Serialize_ShouldIncludeDocumentSettings()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        var settings = new DocumentSettingsDto
        {
            CurrentLayerId = LayerId.Default.Value,
            CurrentTextFormatId = TextFormatId.Title.Value,
            Grid = new DocumentGridSettingsDto
            {
                Kind = "Isometric",
                IsVisible = false,
                MinorStep = 5,
                MajorStep = 25,
                OriginX = 1,
                OriginY = 2,
                MinimumScreenSpacing = 9,
                MaximumScreenSpacing = 180,
                IsometricAngleDegrees = 30
            },
            Snapping = new DocumentSnapSettingsDto
            {
                IsEnabled = true,
                EnabledModes = new List<string> { "Endpoint", "Grid" },
                Tolerance = 12
            },
            Drafting = new DocumentDraftingSettingsDto
            {
                IsOrthoEnabled = true,
                PolarTracking = new DocumentPolarTrackingSettingsDto
                {
                    IsEnabled = false,
                    StepDegrees = 90
                }
            }
        };

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto(),
            settings);

        Assert.Equal("Isometric", dto.Settings.Grid.Kind);
        Assert.False(dto.Settings.Grid.IsVisible);
        Assert.Equal(5, dto.Settings.Grid.MinorStep);
        Assert.Equal(25, dto.Settings.Grid.MajorStep);
        Assert.Equal(new[] { "Endpoint", "Grid" }, dto.Settings.Snapping.EnabledModes);
        Assert.Equal(12, dto.Settings.Snapping.Tolerance);
        Assert.True(dto.Settings.Drafting.IsOrthoEnabled);
        Assert.False(dto.Settings.Drafting.PolarTracking.IsEnabled);
        Assert.Equal(TextFormatId.Title.Value, dto.Settings.CurrentTextFormatId);
    }

    [Fact]
    public void DeserializeWithRecovery_WhenSettingsAreMissing_ShouldUseDefaultSettings()
    {
        var serializer = new JsonDocumentSerializer();
        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = null!
        };

        DocumentRecoveryResult result = serializer.DeserializeWithRecovery(dto);

        Assert.Equal(LayerId.Default.Value, result.Settings.CurrentLayerId);
        Assert.Equal(TextFormatId.Standard.Value, result.Settings.CurrentTextFormatId);
        Assert.True(result.Settings.Grid.IsVisible);
        Assert.Contains("Endpoint", result.Settings.Snapping.EnabledModes);
        Assert.False(result.Settings.Drafting.IsOrthoEnabled);
    }

}

public sealed class JsonDocumentSerializerLineFormatDashPatternTests
{
    [Fact]
    public void Serialize_ShouldPersistLineFormatDashPattern()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        LineFormatId formatId = new("CustomPattern");

        var customFormat = new LineFormat(
            formatId,
            "Custom pattern",
            CadColor.FromRgb(17, 34, 51),
            LineWeight.FromMillimeters(0.75),
            LineStyle.Custom,
            new[] { 10.0, 5.0, 1.0, 5.0 });

        document.ReplaceLineFormats(
            document.LineFormats.WithFormats(
                document.LineFormats.All.Concat(new[] { customFormat })));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        LineFormatDto formatDto = Assert.Single(
            dto.LineFormats,
            format => format.Id == formatId.Value);

        Assert.Equal(nameof(LineStyle.Custom), formatDto.LineStyle);
        Assert.Equal(new[] { 10.0, 5.0, 1.0, 5.0 }, formatDto.DashPattern);
    }

    [Fact]
    public void Deserialize_ShouldRestoreLineFormatDashPattern()
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
                },
                new LineFormatDto
                {
                    Id = "CustomPattern",
                    Name = "Custom pattern",
                    Color = "#112233",
                    LineWeight = 0.75,
                    LineStyle = "Custom",
                    DashPattern = new List<double> { 10.0, 5.0, 1.0, 5.0 }
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
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        LineFormat format = document.LineFormats.GetById(new LineFormatId("CustomPattern"));

        Assert.Equal(LineStyle.Custom, format.LineStyle);
        Assert.Equal(new[] { 10.0, 5.0, 1.0, 5.0 }, format.DashPattern);
    }

    [Fact]
    public void Deserialize_WhenDashPatternIsMissing_ShouldUseStyleDefaultPattern()
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
                },
                new LineFormatDto
                {
                    Id = "LegacyDashed",
                    Name = "Legacy dashed",
                    Color = "#FFFF00",
                    LineWeight = 0.75,
                    LineStyle = "Dashed"
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
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        LineFormat format = document.LineFormats.GetById(new LineFormatId("LegacyDashed"));

        Assert.Equal(new[] { 8.0, 4.0 }, format.DashPattern);
    }

    [Fact]
    public void Deserialize_WhenDashPatternIsInvalid_ShouldFallbackToStyleDefaultPattern()
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
                },
                new LineFormatDto
                {
                    Id = "BrokenDashed",
                    Name = "Broken dashed",
                    Color = "#FFFF00",
                    LineWeight = 0.75,
                    LineStyle = "Dashed",
                    DashPattern = new List<double> { 8.0, -4.0 }
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
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        LineFormat format = document.LineFormats.GetById(new LineFormatId("BrokenDashed"));

        Assert.Equal(new[] { 8.0, 4.0 }, format.DashPattern);
    }
}

public sealed class JsonDocumentSerializerFillTests
{
    [Fact]
    public void Serialize_ShouldPersistLayerFillColor()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        LayerId layerId = new("FillLayer");

        document.Layers.Add(
            new Layer(
                layerId,
                "Fill layer",
                LineFormatId.Continuous,
                fillColor: CadColor.FromRgb(12, 34, 56)));

        DocumentDto dto = serializer.Serialize(
            document,
            layerId.Value,
            new ViewportStateDto());

        LayerDto layerDto = Assert.Single(
            dto.Layers,
            layer => layer.Id == layerId.Value);

        Assert.Equal("#0C2238", layerDto.FillColor);
    }

    [Fact]
    public void Deserialize_ShouldRestoreLayerFillColor()
    {
        var serializer = new JsonDocumentSerializer();
        LayerId layerId = new("FillLayer");

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = layerId.Value
            },
            Layers =
            {
                new LayerDto
                {
                    Id = layerId.Value,
                    Name = "Fill layer",
                    LineFormatId = LineFormatId.Continuous.Value,
                    FillColor = "#0C2238"
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        Layer layer = document.Layers.GetRequired(layerId);

        Assert.Equal(CadColor.FromRgb(12, 34, 56), layer.FillColor);
    }

    [Fact]
    public void Deserialize_LayerWithoutFillColor_ShouldDefaultToLineFormatColor()
    {
        var serializer = new JsonDocumentSerializer();
        LayerId layerId = new("AxisLayer");

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = layerId.Value
            },
            Layers =
            {
                new LayerDto
                {
                    Id = layerId.Value,
                    Name = "Axis layer",
                    LineFormatId = LineFormatId.Axis.Value
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        Layer layer = document.Layers.GetRequired(layerId);

        Assert.Equal(CadColor.FromRgb(255, 0, 0), layer.FillColor);
    }

    [Fact]
    public void Serialize_ShouldPersistFilledCircle()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        var circle = new CircleEntity(
            new Point2D(10, 20),
            30,
            isFilled: true);

        document.AddEntity(circle);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CircleEntityDto circleDto = Assert.IsType<CircleEntityDto>(Assert.Single(dto.Entities));

        Assert.True(circleDto.IsFilled);
    }

    [Fact]
    public void Deserialize_ShouldRestoreFilledCircle()
    {
        var serializer = new JsonDocumentSerializer();
        EntityId entityId = EntityId.New();

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = LayerId.Default.Value
            },
            Entities =
            {
                new CircleEntityDto
                {
                    Id = entityId.ToString(),
                    LayerId = LayerId.Default.Value,
                    CenterX = 10,
                    CenterY = 20,
                    Radius = 30,
                    IsFilled = true
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        CircleEntity circle = Assert.IsType<CircleEntity>(Assert.Single(document.Entities.All));

        Assert.True(circle.IsFilled);
    }

    [Fact]
    public void Serialize_ShouldPersistFilledPolyline()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true,
            isFilled: true);

        document.AddEntity(polyline);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        PolylineEntityDto polylineDto = Assert.IsType<PolylineEntityDto>(Assert.Single(dto.Entities));

        Assert.True(polylineDto.IsFilled);
    }

    [Fact]
    public void Deserialize_ShouldRestoreFilledPolyline()
    {
        var serializer = new JsonDocumentSerializer();
        EntityId entityId = EntityId.New();

        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto
            {
                CurrentLayerId = LayerId.Default.Value
            },
            Entities =
            {
                new PolylineEntityDto
                {
                    Id = entityId.ToString(),
                    LayerId = LayerId.Default.Value,
                    IsClosed = true,
                    IsFilled = true,
                    Vertices =
                    {
                        new PointDto { X = 0, Y = 0 },
                        new PointDto { X = 10, Y = 0 },
                        new PointDto { X = 10, Y = 10 },
                        new PointDto { X = 0, Y = 10 }
                    }
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All));

        Assert.True(polyline.IsFilled);
    }

    [Fact]
    public void Deserialize_EntitiesWithoutIsFilled_ShouldDefaultToFalse()
    {
        const string json = """
        {
          "version": 1,
          "settings": {
            "currentLayerId": "0"
          },
          "entities": [
            {
              "type": "Circle",
              "id": "11111111-1111-1111-1111-111111111111",
              "layerId": "0",
              "centerX": 0,
              "centerY": 0,
              "radius": 5
            },
            {
              "type": "Polyline",
              "id": "22222222-2222-2222-2222-222222222222",
              "layerId": "0",
              "isClosed": true,
              "vertices": [
                { "x": 0, "y": 0 },
                { "x": 10, "y": 0 },
                { "x": 10, "y": 10 }
              ]
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

        Assert.All(document.Entities.All.OfType<IFillableEntity>(), entity =>
            Assert.False(entity.IsFilled));
    }
}
