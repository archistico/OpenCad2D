using OpenCad2D.Core.Architecture.Stairs;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class StairEntityRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveParametricStairEntity()
    {
        var document = new CadDocument();
        var id = new EntityId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var stair = new StairEntity(
            new Point2D(10, 20),
            StairViewKind.SideElevation,
            width: 1.2,
            treadCount: 5,
            treadDepth: 0.3,
            riserHeight: 0.17,
            showStructure: true,
            slabThickness: 0.18,
            xAxis: new Vector2D(0, 1),
            yAxis: new Vector2D(-1, 0),
            id: id);
        document.AddEntity(stair);

        var serializer = new JsonDocumentSerializer();
        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        StairEntity restoredStair = Assert.IsType<StairEntity>(
            restored.Entities.GetRequired(id));

        Assert.Equal(stair.InsertionPoint, restoredStair.InsertionPoint);
        Assert.Equal(StairViewKind.SideElevation, restoredStair.ViewKind);
        Assert.Equal(1.2, restoredStair.Width, precision: 6);
        Assert.Equal(5, restoredStair.TreadCount);
        Assert.Equal(0.3, restoredStair.TreadDepth, precision: 6);
        Assert.Equal(0.17, restoredStair.RiserHeight, precision: 6);
        Assert.True(restoredStair.ShowStructure);
        Assert.Equal(0.18, restoredStair.SlabThickness, precision: 6);
        Assert.Equal(stair.XAxis, restoredStair.XAxis);
        Assert.Equal(stair.YAxis, restoredStair.YAxis);
    }

    [Fact]
    public void Deserialize_WhenStairViewKindIsMissing_ShouldDefaultToPlan()
    {
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
                new StairEntityDto
                {
                    Id = "33333333-3333-3333-3333-333333333333",
                    LayerId = LayerId.Default.Value,
                    ViewKind = "",
                    Width = 1.0,
                    TreadCount = 4,
                    TreadDepth = 0.28,
                    RiserHeight = 0.17
                }
            }
        };

        CadDocument document = serializer.Deserialize(
            dto,
            out _,
            out _);

        StairEntity stair = Assert.IsType<StairEntity>(
            document.Entities.GetRequired(new EntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"))));

        Assert.Equal(StairViewKind.Plan, stair.ViewKind);
    }
}
