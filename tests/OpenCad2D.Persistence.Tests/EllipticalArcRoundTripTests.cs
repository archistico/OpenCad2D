using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class EllipticalArcRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveEllipticalArcEntity()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        var entity = new EllipticalArcEntity(
            new Point2D(10, 20),
            new Vector2D(8, 0),
            3,
            Math.PI / 6.0,
            Math.PI * 0.75,
            isCounterClockwise: true);

        document.AddEntity(entity);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        var restoredEntity = Assert.IsType<EllipticalArcEntity>(
            restored.Entities.GetRequired(entity.Id));
        Assert.Equal(entity.Center, restoredEntity.Center);
        Assert.Equal(entity.MajorAxis, restoredEntity.MajorAxis);
        Assert.Equal(entity.MinorRadius, restoredEntity.MinorRadius);
        Assert.Equal(entity.StartParameterRadians, restoredEntity.StartParameterRadians, 12);
        Assert.Equal(entity.EndParameterRadians, restoredEntity.EndParameterRadians, 12);
        Assert.Equal(entity.IsCounterClockwise, restoredEntity.IsCounterClockwise);
    }

    [Fact]
    public void JsonRoundTrip_ShouldPreserveEllipticalArcDtoType()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        document.AddEntity(new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            4,
            0,
            Math.PI));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        string json = JsonDocumentSerializer.ToJson(dto);
        DocumentDto loadedDto = JsonDocumentSerializer.FromJson(json);

        Assert.Contains(loadedDto.Entities, entity => entity is EllipticalArcEntityDto);
    }
}
