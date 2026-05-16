using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class EndToEndWorkflowTests
{
    [Fact]
    public void DrawAnnotateSaveReopen_ShouldPreservePrimaryEntityTypesAndDocumentState()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        LayerId geometryLayerId = new("Geometry");
        LayerId annotationLayerId = new("Annotations");

        document.Layers.Add(new Layer(geometryLayerId, "Geometry"));
        document.Layers.Add(new Layer(annotationLayerId, "Annotations"));

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            layerId: geometryLayerId);

        var circle = new CircleEntity(
            new Point2D(25, 25),
            10,
            layerId: geometryLayerId);

        var arc = new ArcEntity(
            new Point2D(50, 25),
            12,
            Angle.FromDegrees(0),
            Angle.FromDegrees(135),
            layerId: geometryLayerId);

        var polygon = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(40, 0),
                new Point2D(40, 30),
                new Point2D(0, 30)
            },
            isClosed: true,
            layerId: geometryLayerId);

        var ellipse = new EllipseEntity(
            new Point2D(70, 25),
            new Vector2D(18, 0),
            7,
            layerId: geometryLayerId);

        var spline = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 50),
                new Point2D(20, 70),
                new Point2D(45, 45),
                new Point2D(70, 60)
            },
            layerId: geometryLayerId);

        var text = new TextEntity(
            new Point2D(0, 40),
            "Single line note",
            textFormatId: TextFormatId.Standard,
            layerId: annotationLayerId);

        var multilineText = new MultilineTextEntity(
            new Point2D(0, 48),
            "First line\nSecond line",
            textFormatId: TextFormatId.Standard,
            layerId: annotationLayerId);

        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(50, -12),
            DimensionOrientation.Horizontal,
            layerId: annotationLayerId);

        document.AddEntities(new CadEntity[]
        {
            line,
            circle,
            arc,
            polygon,
            ellipse,
            spline,
            text,
            multilineText,
            dimension
        });

        DocumentDto dto = serializer.Serialize(
            document,
            geometryLayerId.Value,
            new ViewportStateDto
            {
                PanX = 15,
                PanY = -20,
                Zoom = 1.75
            });

        string json = JsonDocumentSerializer.ToJson(dto);
        DocumentDto loadedDto = JsonDocumentSerializer.FromJson(json);
        CadDocument restored = serializer.Deserialize(
            loadedDto,
            out string currentLayerId,
            out ViewportStateDto viewport);

        Assert.Equal(geometryLayerId.Value, currentLayerId);
        Assert.Equal(15, viewport.PanX);
        Assert.Equal(-20, viewport.PanY);
        Assert.Equal(1.75, viewport.Zoom);
        Assert.Equal(9, restored.Entities.Count);

        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.Line);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.Circle);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.Arc);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.Polyline);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.Ellipse);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.BezierSpline);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.Text);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.MultilineText);
        Assert.Contains(restored.Entities.All, entity => entity.Kind == EntityKind.HorizontalDimension);

        var restoredMText = Assert.IsType<MultilineTextEntity>(
            restored.Entities.GetRequired(multilineText.Id));
        Assert.Equal("First line\nSecond line", restoredMText.Text);
        Assert.Equal(annotationLayerId, restoredMText.LayerId);
        Assert.Equal(TextFormatId.Standard, restoredMText.TextFormatId);

        var restoredSpline = Assert.IsType<BezierSplineEntity>(
            restored.Entities.GetRequired(spline.Id));
        Assert.Equal(4, restoredSpline.ControlPoints.Count);
        Assert.False(restoredSpline.IsClosed);
    }
}
