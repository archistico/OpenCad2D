using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Dimensions;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Grips;
using OpenCad2D.Tools.Input;
using OpenCad2D.Tools.Measurements;
using OpenCad2D.Tools.Navigation;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class ToolPreviewEntityProviderTests
{
    [Fact]
    public void LineTool_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();
        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 5)));

        var provider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        var line = Assert.Single(provider.GetPreviewEntities(context).OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 5), line.End);
    }

    [Fact]
    public void MoveTool_ShouldExposeContextAwareEntityPreviewProvider()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var source = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(source);
        selection.Select(source.Id);

        ToolContext context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(3, 4)));

        var provider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        var preview = Assert.Single(provider.GetPreviewEntities(context).OfType<LineEntity>());

        Assert.Equal(new Point2D(3, 4), preview.Start);
        Assert.Equal(new Point2D(13, 4), preview.End);
    }

    [Fact]
    public void ThreePointDimensionTools_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();
        var tool = new HorizontalDimensionTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 3)));

        var provider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        var dimension = Assert.Single(provider.GetPreviewEntities(context).OfType<LinearDimensionEntity>());

        Assert.Equal(DimensionOrientation.Horizontal, dimension.Orientation);
        Assert.Equal(new Point2D(0, 0), dimension.FirstPoint);
        Assert.Equal(new Point2D(10, 0), dimension.SecondPoint);
        Assert.Equal(new Point2D(5, 3), dimension.DimensionLinePoint);
    }


    [Fact]
    public void DrawingTools_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();
        ICadTool[] tools =
        {
            new RectangleTool(),
            new CircleTool(),
            new EllipseTool(),
            new ArcTool(),
            new ArcThreePointsTool(),
            new PolylineTool(),
            new PolygonTool(),
            new SplineTool()
        };

        foreach (ICadTool tool in tools)
        {
            Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        }
    }

    [Fact]
    public void RectangleTool_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 5)));

        var provider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        var rectangle = Assert.Single(provider.GetPreviewEntities(context).OfType<PolylineEntity>());

        Assert.True(rectangle.IsClosed);
        Assert.Equal(4, rectangle.Vertices.Count);
    }

    [Fact]
    public void CircleTool_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();
        var tool = new CircleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(3, 4)));

        var provider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        var circle = Assert.Single(provider.GetPreviewEntities(context).OfType<CircleEntity>());

        Assert.Equal(new Point2D(0, 0), circle.Center);
        Assert.Equal(5, circle.Radius);
    }

    [Fact]
    public void RectangleBySidesTool_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 5)));

        var provider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        var rectangle = Assert.Single(provider.GetPreviewEntities(context).OfType<PolylineEntity>());

        Assert.True(rectangle.IsClosed);
        Assert.Equal(4, rectangle.Vertices.Count);
    }

    [Fact]
    public void CurveDrawingTools_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();

        var ellipseTool = new EllipseTool();
        ellipseTool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ellipseTool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 0)));

        var ellipseProvider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(ellipseTool);
        Assert.Single(ellipseProvider.GetPreviewEntities(context).OfType<EllipseEntity>());

        var arcTool = new ArcTool();
        arcTool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        arcTool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        arcTool.OnPointerMoved(context, new PointerInfo(new Point2D(0, 10)));

        var arcProvider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(arcTool);
        Assert.Single(arcProvider.GetPreviewEntities(context).OfType<ArcEntity>());

        var arcThreePointsTool = new ArcThreePointsTool();
        arcThreePointsTool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        arcThreePointsTool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));
        arcThreePointsTool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 0)));

        var arcThreePointsProvider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(arcThreePointsTool);
        Assert.Single(arcThreePointsProvider.GetPreviewEntities(context).OfType<ArcEntity>());
    }

    [Fact]
    public void PolylinePolygonAndSplineTools_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();

        var polylineTool = new PolylineTool();
        polylineTool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        polylineTool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 0)));

        var polylineProvider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(polylineTool);
        Assert.Single(polylineProvider.GetPreviewEntities(context).OfType<PolylineEntity>());

        var polygonTool = new PolygonTool();
        polygonTool.HandleCommandInput(CommandInputSubmission.Confirm(string.Empty), context);
        polygonTool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        polygonTool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 0)));

        var polygonProvider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(polygonTool);
        PolylineEntity polygon = Assert.Single(polygonProvider.GetPreviewEntities(context).OfType<PolylineEntity>());
        Assert.True(polygon.IsClosed);

        var splineTool = new SplineTool();
        splineTool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        splineTool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 0)));

        var splineProvider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(splineTool);
        Assert.Single(splineProvider.GetPreviewEntities(context).OfType<BezierSplineEntity>());
    }


    [Fact]
    public void ModifyPreviewTools_ShouldExposeEntityPreviewProvider()
    {
        ICadTool[] tools =
        {
            new CopyTool(),
            new RotateTool(),
            new ScaleTool(),
            new AlignTool(),
            new BreakAtPointTool(),
            new BreakBetweenPointsTool(),
            new FilletTool(),
            new OffsetTool(),
            new MeasureDistanceTool()
        };

        foreach (ICadTool tool in tools)
        {
            Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        }
    }

    [Fact]
    public void MeasureDistanceTool_ShouldExposeEntityPreviewProvider()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureDistanceTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(4, 6)));

        var provider = Assert.IsAssignableFrom<IToolPreviewEntityProvider>(tool);
        var preview = Assert.Single(provider.GetPreviewEntities(context).OfType<LineEntity>());

        Assert.Equal(new Point2D(1, 2), preview.Start);
        Assert.Equal(new Point2D(4, 6), preview.End);
    }


    [Fact]
    public void MirrorTool_ShouldExposePreviewDescriptorWithAxisOverlay()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var source = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(source);
        selection.Select(source.Id);

        ToolContext context = CreateContext(document, selection);
        var tool = new MirrorTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 10)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        Assert.Single(descriptor.Entities);
        ToolPreviewLine axis = Assert.Single(descriptor.Lines);
        Assert.Equal(ToolPreviewLineKind.Axis, axis.Kind);
        Assert.Equal(new Point2D(0, 0), axis.Start);
        Assert.Equal(new Point2D(0, 10), axis.End);
        Assert.Equal(2, descriptor.Markers.Count);
    }

    [Fact]
    public void MeasureAngleTool_ShouldExposePreviewDescriptorWithPointMarkers()
    {
        ToolContext context = CreateContext();
        var tool = new MeasureAngleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 10)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        Assert.Equal(2, descriptor.Entities.Count);
        Assert.Equal(2, descriptor.Markers.Count);
        Assert.Contains(
            descriptor.Markers,
            marker => marker.Position == new Point2D(10, 0));
        Assert.Contains(
            descriptor.Markers,
            marker => marker.Position == new Point2D(0, 0));
    }


    [Fact]
    public void SelectionTool_ShouldExposePreviewDescriptorWithSelectionWindow()
    {
        ToolContext context = CreateContext();
        var tool = new SelectionTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 5)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        ToolPreviewWindow window = Assert.Single(descriptor.Windows);
        Assert.Equal(ToolPreviewWindowKind.Selection, window.Kind);
        Assert.Equal(new BoundingBox2D(0, 0, 10, 5), window.Bounds);
    }

    [Fact]
    public void ZoomWindowTool_ShouldExposePreviewDescriptorWithZoomWindow()
    {
        ToolContext context = CreateContext();
        var tool = new ZoomWindowTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 5)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        ToolPreviewWindow window = Assert.Single(descriptor.Windows);
        Assert.Equal(ToolPreviewWindowKind.Zoom, window.Kind);
        Assert.Equal(new BoundingBox2D(0, 0, 10, 5), window.Bounds);
    }


    [Fact]
    public void GripEditTool_ShouldExposePreviewDescriptorWithGripMarkers()
    {
        CadDocument document = new();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        document.AddEntity(line);

        ToolContext context = CreateContext(document);
        var tool = new GripEditTool(
            line.Id,
            new GripProviderRegistry());

        tool.Activate(context);

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        Assert.Empty(descriptor.Entities);
        Assert.Empty(descriptor.Lines);
        Assert.Equal(3, descriptor.Markers.Count);
        Assert.All(
            descriptor.Markers,
            marker =>
            {
                Assert.Equal(ToolPreviewMarkerKind.GripCold, marker.Kind);
                Assert.Equal(ToolPreviewMarkerShape.Square, marker.Shape);
            });
    }

    [Fact]
    public void GripEditTool_WhenMovingGrip_ShouldExposePreviewEntityMeasurementAndWarmGrip()
    {
        CadDocument document = new();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        document.AddEntity(line);

        ToolContext context = CreateContext(document);
        var tool = new GripEditTool(
            line.Id,
            new GripProviderRegistry());

        tool.Activate(context);
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 5)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        Assert.Single(descriptor.Entities);
        ToolPreviewLine measurement = Assert.Single(descriptor.Lines);
        Assert.Equal(ToolPreviewLineKind.Measurement, measurement.Kind);
        Assert.Equal(new Point2D(0, 0), measurement.Start);
        Assert.Equal(new Point2D(0, 5), measurement.End);
        Assert.Contains(
            descriptor.Markers,
            marker => marker.Kind == ToolPreviewMarkerKind.GripWarm);
        Assert.Contains(
            descriptor.Markers,
            marker => marker.Kind == ToolPreviewMarkerKind.Secondary &&
                marker.Position == new Point2D(0, 5));
    }


    [Fact]
    public void ExtendTool_ShouldExposePreviewDescriptorWithHighlightedExtension()
    {
        CadDocument document = new();
        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));
        document.AddEntity(boundary);
        document.AddEntity(target);

        ToolContext context = CreateContext(document);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        var preview = Assert.Single(descriptor.Entities.OfType<LineEntity>());
        var highlighted = Assert.Single(descriptor.HighlightedEntities.OfType<LineEntity>());
        Assert.Equal(new Point2D(0, 0), preview.Start);
        Assert.Equal(new Point2D(10, 0), preview.End);
        Assert.Equal(new Point2D(5, 0), highlighted.Start);
        Assert.Equal(new Point2D(10, 0), highlighted.End);
    }

    [Fact]
    public void TrimTool_ShouldExposePreviewDescriptorWithHighlightedRemovedPart()
    {
        CadDocument document = new();
        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));
        document.AddEntity(boundary);
        document.AddEntity(target);

        ToolContext context = CreateContext(document);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(8, 0)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        Assert.NotEmpty(descriptor.Entities);
        var highlighted = Assert.Single(descriptor.HighlightedEntities.OfType<LineEntity>());
        Assert.Equal(new Point2D(5, 0), highlighted.Start);
        Assert.Equal(new Point2D(10, 0), highlighted.End);
    }

    [Fact]
    public void ToolPreviewDescriptor_ShouldStoreOverlayCollections()
    {
        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(1, 0));
        var highlighted = new LineEntity(
            new Point2D(0, 1),
            new Point2D(1, 1));
        var line = new ToolPreviewLine(
            new Point2D(0, 0),
            new Point2D(0, 1),
            ToolPreviewLineKind.Axis);
        var marker = new ToolPreviewMarker(
            new Point2D(2, 2),
            ToolPreviewMarkerKind.Secondary,
            ToolPreviewMarkerShape.Square);
        var window = new ToolPreviewWindow(
            new BoundingBox2D(0, 0, 5, 5),
            ToolPreviewWindowKind.Zoom);

        var descriptor = new ToolPreviewDescriptor(
            entities: new[] { entity },
            highlightedEntities: new[] { highlighted },
            lines: new[] { line },
            markers: new[] { marker },
            windows: new[] { window });

        Assert.False(descriptor.IsEmpty);
        Assert.Same(entity, Assert.Single(descriptor.Entities));
        Assert.Same(highlighted, Assert.Single(descriptor.HighlightedEntities));
        Assert.Equal(line, Assert.Single(descriptor.Lines));
        Assert.Equal(marker, Assert.Single(descriptor.Markers));
        Assert.Equal(ToolPreviewMarkerShape.Square, Assert.Single(descriptor.Markers).Shape);
        Assert.Equal(window, Assert.Single(descriptor.Windows));
    }


    private static ToolContext CreateContext(
        CadDocument? document = null,
        SelectionSet? selection = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionSet: selection);
    }
}
