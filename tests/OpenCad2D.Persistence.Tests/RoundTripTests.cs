using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class RoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveSupportedEntities()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        LayerId detailLayerId = new("Details");

        document.Layers.Add(
            new Layer(
                detailLayerId,
                "Details"));

        var point = new PointEntity(
            new Point2D(-5, 12),
            layerId: detailLayerId);

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: detailLayerId);

        var circle = new CircleEntity(
            new Point2D(20, 20),
            5,
            layerId: detailLayerId);

        var arc = new ArcEntity(
            new Point2D(30, 30),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90),
            layerId: detailLayerId);

        var ellipse = new EllipseEntity(
            new Point2D(40, 40),
            new Vector2D(12, 0),
            4,
            layerId: detailLayerId);

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(1, 1),
                new Point2D(2, 1),
                new Point2D(2, 2)
            },
            isClosed: true,
            layerId: detailLayerId);

        document.AddEntity(point);
        document.AddEntity(line);
        document.AddEntity(circle);
        document.AddEntity(arc);
        document.AddEntity(ellipse);
        document.AddEntity(polyline);

        DocumentDto dto = serializer.Serialize(
            document,
            detailLayerId.Value,
            new ViewportStateDto
            {
                PanX = 11,
                PanY = 22,
                Zoom = 2.5
            });

        CadDocument restored = serializer.Deserialize(
            dto,
            out string currentLayerId,
            out ViewportStateDto viewport);

        Assert.Equal(detailLayerId.Value, currentLayerId);
        Assert.Equal(11, viewport.PanX);
        Assert.Equal(22, viewport.PanY);
        Assert.Equal(2.5, viewport.Zoom);
        Assert.Equal(6, restored.Entities.Count);

        var restoredPoint = Assert.IsType<PointEntity>(restored.Entities.GetRequired(point.Id));
        Assert.Equal(point.Position, restoredPoint.Position);
        Assert.Equal(detailLayerId, restoredPoint.LayerId);

        var restoredLine = Assert.IsType<LineEntity>(restored.Entities.GetRequired(line.Id));
        Assert.Equal(line.Start, restoredLine.Start);
        Assert.Equal(line.End, restoredLine.End);
        Assert.Equal(detailLayerId, restoredLine.LayerId);

        var restoredCircle = Assert.IsType<CircleEntity>(restored.Entities.GetRequired(circle.Id));
        Assert.Equal(circle.Center, restoredCircle.Center);
        Assert.Equal(circle.Radius, restoredCircle.Radius);

        var restoredArc = Assert.IsType<ArcEntity>(restored.Entities.GetRequired(arc.Id));
        Assert.Equal(arc.Center, restoredArc.Center);
        Assert.Equal(arc.Radius, restoredArc.Radius);
        Assert.Equal(arc.StartAngle.Degrees, restoredArc.StartAngle.Degrees, 8);
        Assert.Equal(arc.EndAngle.Degrees, restoredArc.EndAngle.Degrees, 8);

        var restoredEllipse = Assert.IsType<EllipseEntity>(restored.Entities.GetRequired(ellipse.Id));
        Assert.Equal(ellipse.Center, restoredEllipse.Center);
        Assert.Equal(ellipse.MajorAxis, restoredEllipse.MajorAxis);
        Assert.Equal(ellipse.MinorRadius, restoredEllipse.MinorRadius);

        var restoredPolyline = Assert.IsType<PolylineEntity>(restored.Entities.GetRequired(polyline.Id));
        Assert.True(restoredPolyline.IsClosed);
        Assert.Equal(polyline.Vertices, restoredPolyline.Vertices);
    }

    [Fact]
    public void JsonRoundTrip_ShouldPreserveEntityTypes()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        document.AddEntity(
            new PointEntity(
                new Point2D(-1, -2)));

        document.AddEntity(
            new LineEntity(
                new Point2D(0, 0),
                new Point2D(1, 1)));

        document.AddEntity(
            new CircleEntity(
                new Point2D(5, 5),
                2));

        document.AddEntity(
            new EllipseEntity(
                new Point2D(6, 6),
                new Vector2D(3, 0),
                1));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        string json = JsonDocumentSerializer.ToJson(dto);
        DocumentDto loadedDto = JsonDocumentSerializer.FromJson(json);

        Assert.Contains(loadedDto.Entities, entity => entity is PointEntityDto);
        Assert.Contains(loadedDto.Entities, entity => entity is LineEntityDto);
        Assert.Contains(loadedDto.Entities, entity => entity is CircleEntityDto);
        Assert.Contains(loadedDto.Entities, entity => entity is EllipseEntityDto);
    }
}
