using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;
using Xunit;

namespace OpenCad2D.Persistence.Tests;

public sealed class BlockRoundTripTests
{
    [Fact]
    public void Serialize_ShouldIncludeBlockDefinitionsAndReferences()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        var definition = new BlockDefinition(
            new BlockDefinitionId("NorthArrow"),
            "North Arrow",
            new[] { new LineEntity(new Point2D(0, 0), new Point2D(0, 1)) });
        document.BlockDefinitions.Add(definition);

        document.AddEntity(new BlockReferenceEntity(
            definition.Id,
            new Point2D(10, 20),
            new Vector2D(2, 0),
            new Vector2D(0, 2),
            definition.GetBoundingBox()));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        BlockDefinitionDto definitionDto = Assert.Single(dto.BlockDefinitions);
        Assert.Equal("NorthArrow", definitionDto.Id);
        Assert.Equal("North Arrow", definitionDto.Name);
        Assert.Single(definitionDto.Entities);

        BlockReferenceEntityDto referenceDto = Assert.IsType<BlockReferenceEntityDto>(Assert.Single(dto.Entities));
        Assert.Equal("NorthArrow", referenceDto.BlockDefinitionId);
        Assert.Equal(10, referenceDto.InsertionX);
        Assert.Equal(20, referenceDto.InsertionY);
    }

    [Fact]
    public void Deserialize_ShouldRestoreBlockDefinitionsAndReferences()
    {
        var serializer = new JsonDocumentSerializer();
        var dto = new DocumentDto
        {
            Version = JsonDocumentSerializer.CurrentVersion,
            Settings = new DocumentSettingsDto { CurrentLayerId = LayerId.Default.Value },
            Viewport = new ViewportStateDto(),
            BlockDefinitions =
            {
                new BlockDefinitionDto
                {
                    Id = "Door",
                    Name = "Door",
                    Entities =
                    {
                        new LineEntityDto
                        {
                            Id = EntityId.New().ToString(),
                            LayerId = LayerId.Default.Value,
                            StartX = 0,
                            StartY = 0,
                            EndX = 1,
                            EndY = 0
                        }
                    }
                }
            },
            Entities =
            {
                new BlockReferenceEntityDto
                {
                    Id = EntityId.New().ToString(),
                    LayerId = LayerId.Default.Value,
                    BlockDefinitionId = "Door",
                    InsertionX = 5,
                    InsertionY = 6,
                    XAxisX = 1,
                    YAxisY = 1,
                    DefinitionMinX = 0,
                    DefinitionMinY = 0,
                    DefinitionMaxX = 1,
                    DefinitionMaxY = 1
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        Assert.Single(document.BlockDefinitions.All);
        BlockReferenceEntity reference = Assert.IsType<BlockReferenceEntity>(Assert.Single(document.Entities.All));
        Assert.Equal(new BlockDefinitionId("Door"), reference.BlockDefinitionId);
        Assert.Equal(new Point2D(5, 6), reference.InsertionPoint);
    }
}
