using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfRoundTripTests
{
    [Fact]
    public void ExportThenImport_WithModelCoordinateExport_ShouldPreserveBaseEntities()
    {
        var document = new CadDocument();
        var geometryLayerId = new LayerId("Geometry");
        var notesLayerId = new LayerId("Notes");

        document.Layers.Add(new Layer(
            geometryLayerId,
            "Geometry"));
        document.Layers.Add(new Layer(
            notesLayerId,
            "Notes"));

        document.AddEntity(new LineEntity(
            new Point2D(1, 2),
            new Point2D(30, 40),
            layerId: geometryLayerId));
        document.AddEntity(new CircleEntity(
            new Point2D(5, 6),
            12.5,
            layerId: geometryLayerId));
        document.AddEntity(new PointEntity(
            new Point2D(7, 8),
            layerId: geometryLayerId));
        document.AddEntity(new ArcEntity(
            new Point2D(10, 20),
            15,
            Angle.FromDegrees(30),
            Angle.FromDegrees(120),
            isCounterClockwise: true,
            layerId: geometryLayerId));
        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 20)
            },
            isClosed: true,
            layerId: geometryLayerId));
        document.AddEntity(new TextEntity(
            new Point2D(3, 4),
            "Round trip text",
            rotationDegrees: 45,
            layerId: notesLayerId));

        DxfExportResult exported = new DxfExporter().Export(
            document,
            new DxfExportOptions
            {
                UseCadViewerCoordinateSystem = false
            });

        DxfImportResult imported = new DxfDocumentImporter().Import(exported.Content);

        Assert.False(imported.HasWarnings);
        Assert.False(imported.HasErrors);
        Assert.Equal(6, imported.Statistics.TotalImportedEntities);
        Assert.Equal(1, imported.Statistics.GetImportedEntityCount(EntityKind.Line));
        Assert.Equal(1, imported.Statistics.GetImportedEntityCount(EntityKind.Circle));
        Assert.Equal(1, imported.Statistics.GetImportedEntityCount(EntityKind.Point));
        Assert.Equal(1, imported.Statistics.GetImportedEntityCount(EntityKind.Arc));
        Assert.Equal(1, imported.Statistics.GetImportedEntityCount(EntityKind.Polyline));
        Assert.Equal(1, imported.Statistics.GetImportedEntityCount(EntityKind.Text));

        LineEntity line = imported.Document.Entities.All.OfType<LineEntity>().Single();
        AssertPoint(1, 2, line.Start);
        AssertPoint(30, 40, line.End);
        Assert.Equal(geometryLayerId, line.LayerId);

        CircleEntity circle = imported.Document.Entities.All.OfType<CircleEntity>().Single();
        AssertPoint(5, 6, circle.Center);
        Assert.Equal(12.5, circle.Radius, 6);
        Assert.Equal(geometryLayerId, circle.LayerId);

        PointEntity point = imported.Document.Entities.All.OfType<PointEntity>().Single();
        AssertPoint(7, 8, point.Position);
        Assert.Equal(geometryLayerId, point.LayerId);

        ArcEntity arc = imported.Document.Entities.All.OfType<ArcEntity>().Single();
        AssertPoint(10, 20, arc.Center);
        Assert.Equal(15, arc.Radius, 6);
        Assert.Equal(30, arc.StartAngle.Degrees, 6);
        Assert.Equal(120, arc.EndAngle.Degrees, 6);
        Assert.True(arc.IsCounterClockwise);
        Assert.Equal(geometryLayerId, arc.LayerId);

        PolylineEntity polyline = imported.Document.Entities.All.OfType<PolylineEntity>().Single();
        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
        AssertPoint(0, 0, polyline.Vertices[0]);
        AssertPoint(10, 0, polyline.Vertices[1]);
        AssertPoint(10, 20, polyline.Vertices[2]);
        Assert.Equal(geometryLayerId, polyline.LayerId);

        TextEntity text = imported.Document.Entities.All.OfType<TextEntity>().Single();
        AssertPoint(3, 4, text.InsertionPoint);
        Assert.Equal("Round trip text", text.Text);
        Assert.Equal(45, text.RotationDegrees, 6);
        Assert.Equal(notesLayerId, text.LayerId);
    }

    [Fact]
    public void ExportThenImport_WithLayerTable_ShouldPreserveSupportedLayerState()
    {
        var document = new CadDocument();
        var dashDotLayerId = new LayerId("DashDotLayer");
        var hiddenLockedLayerId = new LayerId("HiddenLocked");

        document.Layers.Add(new Layer(
            dashDotLayerId,
            "DashDotLayer",
            LineFormatId.DashDot));
        document.Layers.Add(new Layer(
            hiddenLockedLayerId,
            "HiddenLocked",
            LineFormatId.Dashed,
            isVisible: false,
            isLocked: true));

        DxfExportResult exported = new DxfExporter().Export(
            document,
            new DxfExportOptions
            {
                UseCadViewerCoordinateSystem = false
            });

        DxfImportResult imported = new DxfDocumentImporter().Import(exported.Content);

        Assert.False(imported.HasWarnings);
        Assert.False(imported.HasErrors);
        Assert.True(imported.Document.Layers.Contains(dashDotLayerId));
        Assert.True(imported.Document.Layers.Contains(hiddenLockedLayerId));

        Layer dashDotLayer = imported.Document.Layers.GetRequired(dashDotLayerId);
        Assert.True(dashDotLayer.IsVisible);
        Assert.False(dashDotLayer.IsLocked);
        Assert.Equal(LineFormatId.DashDot, dashDotLayer.LineFormatId);

        Layer hiddenLockedLayer = imported.Document.Layers.GetRequired(hiddenLockedLayerId);
        Assert.False(hiddenLockedLayer.IsVisible);
        Assert.True(hiddenLockedLayer.IsLocked);
        Assert.Equal(LineFormatId.Dashed, hiddenLockedLayer.LineFormatId);
    }

    [Fact]
    public void Export_WithDefaultCadViewerCoordinateSystem_ShouldKeepInteroperabilityYFlip()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 100)));

        DxfExportResult exported = new DxfExporter().Export(document);
        DxfImportResult imported = new DxfDocumentImporter().Import(exported.Content);

        LineEntity line = imported.Document.Entities.All.OfType<LineEntity>().Single();
        AssertPoint(0, 100, line.Start);
        AssertPoint(10, 0, line.End);
    }


    [Fact]
    public void ExportThenImport_WithMixedPolylineBulges_ShouldPreserveCompoundPolylineTopology()
    {
        var document = new CadDocument();
        var layerId = new LayerId("MixedPolylines");

        document.Layers.Add(new Layer(
            layerId,
            "MixedPolylines"));
        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true,
            layerId: layerId,
            segmentBulges: new[]
            {
                0.0,
                0.5,
                0.0,
                -0.25
            }));

        DxfExportResult exported = new DxfExporter().Export(
            document,
            new DxfExportOptions
            {
                UseCadViewerCoordinateSystem = false
            });

        DxfImportResult imported = new DxfDocumentImporter().Import(exported.Content);

        Assert.False(imported.HasWarnings);
        Assert.False(imported.HasErrors);
        Assert.Equal(1, imported.Statistics.TotalImportedEntities);
        Assert.Equal(1, imported.Statistics.GetImportedEntityCount(EntityKind.Polyline));

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(imported.Document.Entities.All));
        Assert.Equal(layerId, polyline.LayerId);
        Assert.True(polyline.IsClosed);
        Assert.True(polyline.HasArcSegments);
        Assert.Equal(4, polyline.Vertices.Count);
        Assert.Equal(4, polyline.SegmentBulges.Count);
        AssertPoint(0, 0, polyline.Vertices[0]);
        AssertPoint(10, 0, polyline.Vertices[1]);
        AssertPoint(10, 10, polyline.Vertices[2]);
        AssertPoint(0, 10, polyline.Vertices[3]);
        Assert.Equal(0, polyline.SegmentBulges[0], 6);
        Assert.Equal(0.5, polyline.SegmentBulges[1], 6);
        Assert.Equal(0, polyline.SegmentBulges[2], 6);
        Assert.Equal(-0.25, polyline.SegmentBulges[3], 6);
    }

    private static void AssertPoint(
        double expectedX,
        double expectedY,
        Point2D actual)
    {
        Assert.Equal(expectedX, actual.X, 6);
        Assert.Equal(expectedY, actual.Y, 6);
    }
}
