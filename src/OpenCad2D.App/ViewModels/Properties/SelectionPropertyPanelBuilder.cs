using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenCad2D.Core.Architecture.Stairs;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.App.ViewModels.Properties;

public sealed class SelectionPropertyPanelBuilder
{
    private const int MaxPolylineVerticesInPropertyPanel = 4;
    private const int MaxPolylineSegmentsInPropertyPanel = 8;
    private static readonly string[] YesNoOptions = ["Yes", "No"];
    private static readonly string[] FillOptions = ["None", "Solid"];
    private static readonly string[] StairViewOptions = ["Plan", "Side elevation", "Front elevation"];
    public PropertyPanelViewModel Build(
        CadWorkspace workspace,
        Action<string>? setMessage = null,
        Action? refresh = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        IReadOnlyList<CadEntity> selectedEntities = workspace.SelectionSet.SelectedIds
            .Where(workspace.Document.Entities.Contains)
            .Select(workspace.Document.Entities.GetRequired)
            .ToList();

        return selectedEntities.Count switch
        {
            0 => BuildNoSelectionPanel(workspace),
            1 => BuildSingleSelectionPanel(workspace, selectedEntities[0], setMessage, refresh),
            _ => BuildMultipleSelectionPanel(workspace, selectedEntities)
        };
    }

    private static PropertyPanelViewModel BuildNoSelectionPanel(
        CadWorkspace workspace)
    {
        var sections = new[]
        {
            new PropertySectionViewModel(
                "Selection",
                new[]
                {
                    Row("State", "No selection"),
                    Row("Selected", "0")
                }),
            new PropertySectionViewModel(
                "Document",
                new[]
                {
                    Row("Entities", workspace.Document.Entities.Count.ToString(CultureInfo.InvariantCulture)),
                    Row("Layers", workspace.Document.Layers.Count.ToString(CultureInfo.InvariantCulture)),
                    Row("Current layer", workspace.Document.Layers.GetRequired(workspace.CurrentLayerId).Name)
                })
        };

        return new PropertyPanelViewModel(
            "Properties",
            sections);
    }

    private static PropertyPanelViewModel BuildSingleSelectionPanel(
        CadWorkspace workspace,
        CadEntity entity,
        Action<string>? setMessage,
        Action? refresh)
    {
        Layer layer = workspace.Document.Layers.GetRequired(entity.LayerId);

        var sections = new List<PropertySectionViewModel>
        {
            new(
                "Selection",
                new[]
                {
                    Row("Type", GetEntityTypeName(entity)),
                    Row("Layer", layer.Name),
                    ComboRow(
                        "Layer id",
                        entity.LayerId.Value,
                        GetLayerIdOptions(workspace),
                        value => ReplaceEntityLayer(
                            workspace,
                            entity.Id,
                            value,
                            setMessage,
                            refresh)),
                    Row("Id", entity.Id.ToString()),
                    Row("Draw order", entity.DrawOrder.ToString(CultureInfo.InvariantCulture)),
                    Row("Visible", PropertyValueFormatter.FormatBoolean(workspace.Document.IsEntityVisible(entity))),
                    Row("Layer locked", PropertyValueFormatter.FormatBoolean(layer.IsLocked))
                })
        };

        sections.Add(BuildGeometrySection(workspace, entity, setMessage, refresh));

        if (entity is PolylineEntity polyline)
        {
            sections.Add(BuildPolylineVerticesSection(workspace, polyline, setMessage, refresh));
            sections.Add(BuildPolylineSegmentsSection(workspace, polyline, setMessage, refresh));
        }

        sections.Add(BuildBoundsSection(entity.GetBoundingBox()));

        return new PropertyPanelViewModel(
            GetEntityTypeName(entity),
            sections);
    }

    private static PropertyPanelViewModel BuildMultipleSelectionPanel(
        CadWorkspace workspace,
        IReadOnlyList<CadEntity> selectedEntities)
    {
        var typeRows = selectedEntities
            .GroupBy(GetEntityTypeName)
            .OrderBy(group => group.Key)
            .Select(group => Row(group.Key, group.Count().ToString(CultureInfo.InvariantCulture)))
            .ToList();

        var layerRows = selectedEntities
            .GroupBy(entity => workspace.Document.Layers.GetRequired(entity.LayerId).Name)
            .OrderBy(group => group.Key)
            .Select(group => Row(group.Key, group.Count().ToString(CultureInfo.InvariantCulture)))
            .ToList();

        var sections = new List<PropertySectionViewModel>
        {
            new(
                "Selection",
                new[]
                {
                    Row("State", "Multiple selection"),
                    Row("Selected", selectedEntities.Count.ToString(CultureInfo.InvariantCulture))
                }),
            new("Types", typeRows),
            new("Layers", layerRows)
        };

        BoundingBox2D? bounds = CombineBounds(selectedEntities);

        if (bounds is not null)
        {
            sections.Add(BuildBoundsSection(bounds.Value));
        }

        return new PropertyPanelViewModel(
            "Multiple selection",
            sections);
    }

    private static PropertySectionViewModel BuildGeometrySection(
        CadWorkspace workspace,
        CadEntity entity,
        Action<string>? setMessage,
        Action? refresh)
    {
        return entity switch
        {
            PointEntity point => BuildPointGeometrySection(workspace, point, setMessage, refresh),
            TextEntity text => BuildTextGeometrySection(workspace, text, setMessage, refresh),
            MultilineTextEntity multilineText => BuildMultilineTextGeometrySection(workspace, multilineText, setMessage, refresh),
            LinearDimensionEntity linearDimension => BuildLinearDimensionGeometrySection(workspace, linearDimension, setMessage, refresh),
            AlignedDimensionEntity alignedDimension => BuildAlignedDimensionGeometrySection(workspace, alignedDimension, setMessage, refresh),
            RadiusDimensionEntity radiusDimension => BuildRadiusDimensionGeometrySection(workspace, radiusDimension, setMessage, refresh),
            DiameterDimensionEntity diameterDimension => BuildDiameterDimensionGeometrySection(workspace, diameterDimension, setMessage, refresh),
            AngularDimensionEntity angularDimension => BuildAngularDimensionGeometrySection(workspace, angularDimension, setMessage, refresh),
            LineEntity line => BuildLineGeometrySection(workspace, line, setMessage, refresh),
            CircleEntity circle => BuildCircleGeometrySection(workspace, circle, setMessage, refresh),
            EllipseEntity ellipse => BuildEllipseGeometrySection(ellipse),
            EllipticalArcEntity ellipticalArc => BuildEllipticalArcGeometrySection(ellipticalArc),
            PolylineEntity polyline => BuildPolylineGeometrySection(workspace, polyline, setMessage, refresh),
            BezierSplineEntity spline => BuildBezierSplineGeometrySection(spline),
            ImageReferenceEntity imageReference => BuildImageReferenceGeometrySection(workspace, imageReference, setMessage, refresh),
            StairEntity stair => BuildStairGeometrySection(workspace, stair, setMessage, refresh),
            ArcEntity arc => BuildArcGeometrySection(workspace, arc, setMessage, refresh),
            _ => new PropertySectionViewModel(
                "Geometry",
                new[] { Row("Details", "Not available") })
        };
    }

    private static PropertySectionViewModel BuildPointGeometrySection(
        CadWorkspace workspace,
        PointEntity point,
        Action<string>? setMessage,
        Action? refresh)
    {
        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Position", PropertyValueFormatter.FormatPoint(point.Position)),
                EditableRow(
                    "X",
                    PropertyValueFormatter.FormatCoordinate(point.Position.X),
                    value => ReplacePointCoordinate(workspace, point.Id, value, updateX: true, setMessage, refresh)),
                EditableRow(
                    "Y",
                    PropertyValueFormatter.FormatCoordinate(point.Position.Y),
                    value => ReplacePointCoordinate(workspace, point.Id, value, updateX: false, setMessage, refresh))
            });
    }

    private static PropertySectionViewModel BuildTextGeometrySection(
        CadWorkspace workspace,
        TextEntity text,
        Action<string>? setMessage,
        Action? refresh)
    {
        return new PropertySectionViewModel(
            "Text",
            new[]
            {
                EditableRow("Value", text.Text, value => ReplaceTextValue(workspace, text.Id, value, setMessage, refresh)),
                Row("Insertion", PropertyValueFormatter.FormatPoint(text.InsertionPoint)),
                EditableRow("X", PropertyValueFormatter.FormatCoordinate(text.InsertionPoint.X), value => ReplaceTextCoordinate(workspace, text.Id, value, updateX: true, setMessage, refresh)),
                EditableRow("Y", PropertyValueFormatter.FormatCoordinate(text.InsertionPoint.Y), value => ReplaceTextCoordinate(workspace, text.Id, value, updateX: false, setMessage, refresh)),
                EditableRow("Rotation", PropertyValueFormatter.FormatCoordinate(text.RotationDegrees), value => ReplaceTextRotation(workspace, text.Id, value, setMessage, refresh)),
                EditableRow("Text format", text.TextFormatId.Value, value => ReplaceTextFormat(workspace, text.Id, value, setMessage, refresh))
            });
    }


    private static PropertySectionViewModel BuildMultilineTextGeometrySection(
        CadWorkspace workspace,
        MultilineTextEntity text,
        Action<string>? setMessage,
        Action? refresh)
    {
        return new PropertySectionViewModel(
            "Multiline Text",
            new[]
            {
                EditableRow("Value", text.Text, value => ReplaceMultilineTextValue(workspace, text.Id, value, setMessage, refresh)),
                Row("Insertion", PropertyValueFormatter.FormatPoint(text.InsertionPoint)),
                EditableRow("X", PropertyValueFormatter.FormatCoordinate(text.InsertionPoint.X), value => ReplaceMultilineTextCoordinate(workspace, text.Id, value, updateX: true, setMessage, refresh)),
                EditableRow("Y", PropertyValueFormatter.FormatCoordinate(text.InsertionPoint.Y), value => ReplaceMultilineTextCoordinate(workspace, text.Id, value, updateX: false, setMessage, refresh)),
                EditableRow("Rotation", PropertyValueFormatter.FormatCoordinate(text.RotationDegrees), value => ReplaceMultilineTextRotation(workspace, text.Id, value, setMessage, refresh)),
                EditableRow("Text format", text.TextFormatId.Value, value => ReplaceMultilineTextFormat(workspace, text.Id, value, setMessage, refresh)),
                EditableRow("Reference width", PropertyValueFormatter.FormatCoordinate(text.ReferenceWidth), value => ReplaceMultilineTextReferenceWidth(workspace, text.Id, value, setMessage, refresh)),
                Row("Lines", text.Lines.Count.ToString(CultureInfo.InvariantCulture))
            });
    }

    private static PropertySectionViewModel BuildLinearDimensionGeometrySection(
        CadWorkspace workspace,
        LinearDimensionEntity dimension,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>
        {
            Row("Kind", dimension.Orientation == DimensionOrientation.Horizontal ? "Horizontal" : "Vertical"),
            Row("First point", PropertyValueFormatter.FormatPoint(dimension.FirstPoint)),
            Row("Second point", PropertyValueFormatter.FormatPoint(dimension.SecondPoint)),
            Row("Dimension line", PropertyValueFormatter.FormatPoint(dimension.DimensionLinePoint)),
            Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue))
        };

        AddDimensionEditableRows(workspace, rows, dimension, setMessage, refresh);

        return new PropertySectionViewModel("Dimension", rows);
    }

    private static PropertySectionViewModel BuildAlignedDimensionGeometrySection(
        CadWorkspace workspace,
        AlignedDimensionEntity dimension,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>
        {
            Row("Kind", "Aligned"),
            Row("First point", PropertyValueFormatter.FormatPoint(dimension.FirstPoint)),
            Row("Second point", PropertyValueFormatter.FormatPoint(dimension.SecondPoint)),
            Row("Dimension line", PropertyValueFormatter.FormatPoint(dimension.DimensionLinePoint)),
            Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue))
        };

        AddDimensionEditableRows(workspace, rows, dimension, setMessage, refresh);

        return new PropertySectionViewModel("Dimension", rows);
    }

    private static PropertySectionViewModel BuildRadiusDimensionGeometrySection(
        CadWorkspace workspace,
        RadiusDimensionEntity dimension,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>
        {
            Row("Kind", "Radius"),
            Row("Center", PropertyValueFormatter.FormatPoint(dimension.Center)),
            Row("Point on circle", PropertyValueFormatter.FormatPoint(dimension.PointOnCircle)),
            Row("Text point", PropertyValueFormatter.FormatPoint(dimension.TextPoint)),
            Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue))
        };

        AddDimensionEditableRows(workspace, rows, dimension, setMessage, refresh);

        return new PropertySectionViewModel("Dimension", rows);
    }

    private static PropertySectionViewModel BuildDiameterDimensionGeometrySection(
        CadWorkspace workspace,
        DiameterDimensionEntity dimension,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>
        {
            Row("Kind", "Diameter"),
            Row("Center", PropertyValueFormatter.FormatPoint(dimension.Center)),
            Row("Point on circle", PropertyValueFormatter.FormatPoint(dimension.PointOnCircle)),
            Row("Opposite point", PropertyValueFormatter.FormatPoint(dimension.OppositePoint)),
            Row("Text point", PropertyValueFormatter.FormatPoint(dimension.TextPoint)),
            Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue))
        };

        AddDimensionEditableRows(workspace, rows, dimension, setMessage, refresh);

        return new PropertySectionViewModel("Dimension", rows);
    }

    private static PropertySectionViewModel BuildAngularDimensionGeometrySection(
        CadWorkspace workspace,
        AngularDimensionEntity dimension,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>
        {
            Row("Kind", "Angular"),
            Row("Center", PropertyValueFormatter.FormatPoint(dimension.Center)),
            Row("First ray", PropertyValueFormatter.FormatPoint(dimension.FirstRayPoint)),
            Row("Second ray", PropertyValueFormatter.FormatPoint(dimension.SecondRayPoint)),
            Row("Arc point", PropertyValueFormatter.FormatPoint(dimension.ArcPoint)),
            Row("Direction", dimension.IsCounterClockwise ? "Counter-clockwise" : "Clockwise"),
            Row("Measurement", PropertyValueFormatter.FormatAngleDegrees(dimension.MeasurementValue))
        };

        AddDimensionEditableRows(workspace, rows, dimension, setMessage, refresh);

        return new PropertySectionViewModel("Dimension", rows);
    }

    private static void AddDimensionEditableRows(
        CadWorkspace workspace,
        List<PropertyRowViewModel> rows,
        DimensionEntity dimension,
        Action<string>? setMessage,
        Action? refresh)
    {
        rows.Add(Row(
            "Status",
            dimension.IsStale ? "Potentially stale" : "Checked"));
        rows.Add(ComboRow(
            "Dimension style",
            GetDimensionStyleDisplayName(workspace, dimension.DimensionStyleId),
            workspace.Document.DimensionStyles.All.Select(style => style.Name),
            value => ReplaceDimensionStyle(workspace, dimension.Id, value, setMessage, refresh)));
        rows.Add(EditableRow(
            "Text override",
            dimension.TextOverride ?? string.Empty,
            value => ReplaceDimensionTextOverride(workspace, dimension.Id, value, setMessage, refresh)));
    }

    private static PropertySectionViewModel BuildLineGeometrySection(
        CadWorkspace workspace,
        LineEntity line,
        Action<string>? setMessage,
        Action? refresh)
    {
        Vector2D delta = line.Start.VectorTo(line.End);
        double angleDegrees = Math.Atan2(delta.Y, delta.X) * 180.0 / Math.PI;

        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Start", PropertyValueFormatter.FormatPoint(line.Start)),
                EditableRow("Start X", PropertyValueFormatter.FormatCoordinate(line.Start.X), value => ReplaceLineCoordinate(workspace, line.Id, value, pointName: "start", updateX: true, setMessage, refresh)),
                EditableRow("Start Y", PropertyValueFormatter.FormatCoordinate(line.Start.Y), value => ReplaceLineCoordinate(workspace, line.Id, value, pointName: "start", updateX: false, setMessage, refresh)),
                Row("End", PropertyValueFormatter.FormatPoint(line.End)),
                EditableRow("End X", PropertyValueFormatter.FormatCoordinate(line.End.X), value => ReplaceLineCoordinate(workspace, line.Id, value, pointName: "end", updateX: true, setMessage, refresh)),
                EditableRow("End Y", PropertyValueFormatter.FormatCoordinate(line.End.Y), value => ReplaceLineCoordinate(workspace, line.Id, value, pointName: "end", updateX: false, setMessage, refresh)),
                Row("Length", PropertyValueFormatter.FormatLength(delta.Length)),
                Row("DX", PropertyValueFormatter.FormatLength(delta.X)),
                Row("DY", PropertyValueFormatter.FormatLength(delta.Y)),
                Row("Angle", PropertyValueFormatter.FormatAngleDegrees(angleDegrees))
            });
    }

    private static PropertySectionViewModel BuildCircleGeometrySection(
        CadWorkspace workspace,
        CircleEntity circle,
        Action<string>? setMessage,
        Action? refresh)
    {
        double diameter = circle.Radius * 2.0;
        double area = Math.PI * circle.Radius * circle.Radius;
        double circumference = 2.0 * Math.PI * circle.Radius;

        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Center", PropertyValueFormatter.FormatPoint(circle.Center)),
                EditableRow("Center X", PropertyValueFormatter.FormatCoordinate(circle.Center.X), value => ReplaceCircleCoordinate(workspace, circle.Id, value, updateX: true, setMessage, refresh)),
                EditableRow("Center Y", PropertyValueFormatter.FormatCoordinate(circle.Center.Y), value => ReplaceCircleCoordinate(workspace, circle.Id, value, updateX: false, setMessage, refresh)),
                EditableRow("Radius", PropertyValueFormatter.FormatCoordinate(circle.Radius), value => ReplaceCircleRadius(workspace, circle.Id, value, setMessage, refresh)),
                ComboRow("Fill", FormatFill(circle.IsFilled), FillOptions, value => ReplaceCircleFill(workspace, circle.Id, value, setMessage, refresh)),
                Row("Diameter", PropertyValueFormatter.FormatLength(diameter)),
                Row("Area", PropertyValueFormatter.FormatArea(area)),
                Row("Circumference", PropertyValueFormatter.FormatLength(circumference))
            });
    }

    private static PropertySectionViewModel BuildEllipseGeometrySection(EllipseEntity ellipse)
    {
        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Center", PropertyValueFormatter.FormatPoint(ellipse.Center)),
                Row("Major radius", PropertyValueFormatter.FormatLength(ellipse.MajorRadius)),
                Row("Minor radius", PropertyValueFormatter.FormatLength(ellipse.MinorRadius)),
                Row("Rotation", PropertyValueFormatter.FormatAngleDegrees(ellipse.RotationDegrees)),
                Row("Bounds", $"{PropertyValueFormatter.FormatLength(ellipse.GetBoundingBox().Width)} × {PropertyValueFormatter.FormatLength(ellipse.GetBoundingBox().Height)}")
            });
    }

    private static PropertySectionViewModel BuildEllipticalArcGeometrySection(EllipticalArcEntity ellipticalArc)
    {
        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Center", PropertyValueFormatter.FormatPoint(ellipticalArc.Center)),
                Row("Major radius", PropertyValueFormatter.FormatLength(ellipticalArc.MajorRadius)),
                Row("Minor radius", PropertyValueFormatter.FormatLength(ellipticalArc.MinorRadius)),
                Row("Rotation", PropertyValueFormatter.FormatAngleDegrees(ellipticalArc.RotationRadians * 180.0 / Math.PI)),
                Row("Start parameter", PropertyValueFormatter.FormatAngleDegrees(ellipticalArc.StartParameterRadians * 180.0 / Math.PI)),
                Row("End parameter", PropertyValueFormatter.FormatAngleDegrees(ellipticalArc.EndParameterRadians * 180.0 / Math.PI)),
                Row("Sweep", PropertyValueFormatter.FormatAngleDegrees(ellipticalArc.SweepRadians * 180.0 / Math.PI)),
                Row("Direction", ellipticalArc.IsCounterClockwise ? "Counter-clockwise" : "Clockwise")
            });
    }

    private static PropertySectionViewModel BuildPolylineGeometrySection(
        CadWorkspace workspace,
        PolylineEntity polyline,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>
        {
            Row("Vertices", polyline.Vertices.Count.ToString(CultureInfo.InvariantCulture)),
            new PropertyRowViewModel(
                "Closed",
                FormatYesNo(polyline.IsClosed),
                isEditable: true,
                value => ReplacePolylineClosed(workspace, polyline.Id, value, setMessage, refresh),
                YesNoOptions)
        };

        if (polyline.IsClosed)
        {
            rows.Add(ComboRow(
                "Fill",
                FormatFill(polyline.IsFilled),
                FillOptions,
                value => ReplacePolylineFill(workspace, polyline.Id, value, setMessage, refresh)));
        }

        rows.Add(Row("Segments", polyline.SegmentCount.ToString(CultureInfo.InvariantCulture)));
        rows.Add(Row("Arc segments", polyline.SegmentBulges.Count(bulge => !OpenCad2D.Geometry.Tolerance.IsZero(bulge)).ToString(CultureInfo.InvariantCulture)));
        rows.Add(Row("Length", PropertyValueFormatter.FormatLength(GetPolylineLength(polyline))));

        if (polyline.IsClosed && polyline.Vertices.Count >= 3)
        {
            rows.Add(Row("Area", PropertyValueFormatter.FormatArea(Math.Abs(GetPolylineSignedArea(polyline)))));
        }

        return new PropertySectionViewModel(
            "Geometry",
            rows);
    }

    private static PropertySectionViewModel BuildPolylineVerticesSection(
        CadWorkspace workspace,
        PolylineEntity polyline,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>();
        int displayedVertexCount = Math.Min(
            polyline.Vertices.Count,
            MaxPolylineVerticesInPropertyPanel);

        for (int index = 0; index < displayedVertexCount; index++)
        {
            Point2D vertex = polyline.Vertices[index];
            int vertexIndex = index;

            rows.Add(EditableRow(
                $"Vertex {index + 1}",
                FormatVertexValue(vertex),
                value => ReplacePolylineVertex(
                    workspace,
                    polyline.Id,
                    vertexIndex,
                    value,
                    setMessage,
                    refresh)));
        }

        if (polyline.Vertices.Count > displayedVertexCount)
        {
            rows.Add(Row(
                "More vertices",
                $"{polyline.Vertices.Count - displayedVertexCount} hidden to keep the Property Panel responsive."));
        }

        if (rows.Count == 0)
        {
            rows.Add(Row("Vertices", "No vertices"));
        }

        return new PropertySectionViewModel(
            "Vertices",
            rows);
    }

    private static PropertySectionViewModel BuildPolylineSegmentsSection(
        CadWorkspace workspace,
        PolylineEntity polyline,
        Action<string>? setMessage,
        Action? refresh)
    {
        var rows = new List<PropertyRowViewModel>();
        int displayedSegmentCount = Math.Min(
            polyline.SegmentCount,
            MaxPolylineSegmentsInPropertyPanel);

        for (int index = 0; index < displayedSegmentCount; index++)
        {
            int segmentIndex = index;
            double bulge = polyline.SegmentBulges[index];

            rows.Add(EditableRow(
                $"Segment {index + 1} bulge",
                PropertyValueFormatter.FormatCoordinate(bulge),
                value => ReplacePolylineSegmentBulge(
                    workspace,
                    polyline.Id,
                    segmentIndex,
                    value,
                    setMessage,
                    refresh)));
        }

        if (polyline.SegmentCount > displayedSegmentCount)
        {
            rows.Add(Row(
                "More segments",
                $"{polyline.SegmentCount - displayedSegmentCount} hidden to keep the Property Panel responsive."));
        }

        if (rows.Count == 0)
        {
            rows.Add(Row("Segments", "No editable segments"));
        }

        return new PropertySectionViewModel(
            "Segments",
            rows);
    }

    private static PropertySectionViewModel BuildBezierSplineGeometrySection(BezierSplineEntity spline)
    {
        PolylineEntity approximation = spline.ToPolylineApproximation();
        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Control points", spline.ControlPoints.Count.ToString(CultureInfo.InvariantCulture)),
                Row("Closed", PropertyValueFormatter.FormatBoolean(spline.IsClosed)),
                Row("Approx. samples", approximation.Vertices.Count.ToString(CultureInfo.InvariantCulture)),
                Row("Approx. length", PropertyValueFormatter.FormatLength(GetPolylineLength(approximation)))
            });
    }


    private static PropertySectionViewModel BuildImageReferenceGeometrySection(
        CadWorkspace workspace,
        ImageReferenceEntity imageReference,
        Action<string>? setMessage,
        Action? refresh)
    {
        double area = Math.Abs(imageReference.WidthVector.Cross(imageReference.HeightVector));

        return new PropertySectionViewModel(
            "Image Reference",
            new[]
            {
                EditableRow("File", imageReference.FilePath, value => ReplaceImageReferenceFilePath(workspace, imageReference.Id, value, setMessage, refresh)),
                Row("Origin", PropertyValueFormatter.FormatPoint(imageReference.Origin)),
                EditableRow("Origin X", PropertyValueFormatter.FormatCoordinate(imageReference.Origin.X), value => ReplaceImageReferenceOriginCoordinate(workspace, imageReference.Id, value, updateX: true, setMessage, refresh)),
                EditableRow("Origin Y", PropertyValueFormatter.FormatCoordinate(imageReference.Origin.Y), value => ReplaceImageReferenceOriginCoordinate(workspace, imageReference.Id, value, updateX: false, setMessage, refresh)),
                Row("Center", PropertyValueFormatter.FormatPoint(imageReference.Center)),
                EditableRow("Width", PropertyValueFormatter.FormatLength(imageReference.Width), value => ReplaceImageReferenceWidth(workspace, imageReference.Id, value, setMessage, refresh)),
                EditableRow("Height", PropertyValueFormatter.FormatLength(imageReference.Height), value => ReplaceImageReferenceHeight(workspace, imageReference.Id, value, setMessage, refresh)),
                EditableRow("Rotation", PropertyValueFormatter.FormatCoordinate(imageReference.RotationDegrees), value => ReplaceImageReferenceRotation(workspace, imageReference.Id, value, setMessage, refresh)),
                EditableRow("Transparency %", imageReference.TransparencyPercent.ToString("0.#", CultureInfo.InvariantCulture), value => ReplaceImageReferenceTransparency(workspace, imageReference.Id, value, setMessage, refresh)),
                Row("Area", PropertyValueFormatter.FormatArea(area)),
                Row("Pixels", $"{imageReference.PixelWidth} x {imageReference.PixelHeight}"),
                Row("Natural aspect", imageReference.HasNaturalAspectRatio
                    ? imageReference.NaturalAspectRatio.ToString("0.######", CultureInfo.InvariantCulture)
                    : "Unknown")
            });
    }

    private static PropertySectionViewModel BuildStairGeometrySection(
        CadWorkspace workspace,
        StairEntity stair,
        Action<string>? setMessage,
        Action? refresh)
    {
        return new PropertySectionViewModel(
            "Stair",
            new[]
            {
                ComboRow("View", FormatStairView(stair.ViewKind), StairViewOptions, value => ReplaceStairView(workspace, stair.Id, value, setMessage, refresh)),
                Row("Insertion", PropertyValueFormatter.FormatPoint(stair.InsertionPoint)),
                EditableRow("Insertion X", PropertyValueFormatter.FormatCoordinate(stair.InsertionPoint.X), value => ReplaceStairInsertionCoordinate(workspace, stair.Id, value, updateX: true, setMessage, refresh)),
                EditableRow("Insertion Y", PropertyValueFormatter.FormatCoordinate(stair.InsertionPoint.Y), value => ReplaceStairInsertionCoordinate(workspace, stair.Id, value, updateX: false, setMessage, refresh)),
                EditableRow("Width", PropertyValueFormatter.FormatLength(stair.Width), value => ReplaceStairWidth(workspace, stair.Id, value, setMessage, refresh)),
                EditableRow("Tread count", stair.TreadCount.ToString(CultureInfo.InvariantCulture), value => ReplaceStairTreadCount(workspace, stair.Id, value, setMessage, refresh)),
                EditableRow("Tread depth", PropertyValueFormatter.FormatLength(stair.TreadDepth), value => ReplaceStairTreadDepth(workspace, stair.Id, value, setMessage, refresh)),
                EditableRow("Riser height", PropertyValueFormatter.FormatLength(stair.RiserHeight), value => ReplaceStairRiserHeight(workspace, stair.Id, value, setMessage, refresh)),
                Row("Total run", PropertyValueFormatter.FormatLength(stair.TotalRun)),
                Row("Total rise", PropertyValueFormatter.FormatLength(stair.TotalRise)),
                ComboRow("Show structure", FormatYesNo(stair.ShowStructure), YesNoOptions, value => ReplaceStairShowStructure(workspace, stair.Id, value, setMessage, refresh)),
                EditableRow("Slab thickness", PropertyValueFormatter.FormatLength(stair.SlabThickness), value => ReplaceStairSlabThickness(workspace, stair.Id, value, setMessage, refresh))
            });
    }

    private static PropertySectionViewModel BuildArcGeometrySection(
        CadWorkspace workspace,
        ArcEntity arc,
        Action<string>? setMessage,
        Action? refresh)
    {
        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Center", PropertyValueFormatter.FormatPoint(arc.Center)),
                EditableRow("Center X", PropertyValueFormatter.FormatCoordinate(arc.Center.X), value => ReplaceArcCoordinate(workspace, arc.Id, value, updateX: true, setMessage, refresh)),
                EditableRow("Center Y", PropertyValueFormatter.FormatCoordinate(arc.Center.Y), value => ReplaceArcCoordinate(workspace, arc.Id, value, updateX: false, setMessage, refresh)),
                EditableRow("Radius", PropertyValueFormatter.FormatCoordinate(arc.Radius), value => ReplaceArcRadius(workspace, arc.Id, value, setMessage, refresh)),
                EditableRow("Start angle", PropertyValueFormatter.FormatCoordinate(arc.StartAngle.Degrees), value => ReplaceArcAngle(workspace, arc.Id, value, updateStartAngle: true, setMessage, refresh)),
                EditableRow("End angle", PropertyValueFormatter.FormatCoordinate(arc.EndAngle.Degrees), value => ReplaceArcAngle(workspace, arc.Id, value, updateStartAngle: false, setMessage, refresh)),
                Row("Direction", arc.IsCounterClockwise ? "Counter-clockwise" : "Clockwise")
            });
    }

    private static PropertySectionViewModel BuildBoundsSection(BoundingBox2D bounds)
    {
        return new PropertySectionViewModel(
            "Bounds",
            new[]
            {
                Row("Min", $"X {PropertyValueFormatter.FormatCoordinate(bounds.MinX)}, Y {PropertyValueFormatter.FormatCoordinate(bounds.MinY)}"),
                Row("Max", $"X {PropertyValueFormatter.FormatCoordinate(bounds.MaxX)}, Y {PropertyValueFormatter.FormatCoordinate(bounds.MaxY)}"),
                Row("Width", PropertyValueFormatter.FormatLength(bounds.Width)),
                Row("Height", PropertyValueFormatter.FormatLength(bounds.Height))
            });
    }

    private static BoundingBox2D? CombineBounds(IReadOnlyList<CadEntity> entities)
    {
        if (entities.Count == 0)
        {
            return null;
        }

        BoundingBox2D first = entities[0].GetBoundingBox();
        double minX = first.MinX;
        double minY = first.MinY;
        double maxX = first.MaxX;
        double maxY = first.MaxY;

        foreach (CadEntity entity in entities.Skip(1))
        {
            BoundingBox2D bounds = entity.GetBoundingBox();

            minX = Math.Min(minX, bounds.MinX);
            minY = Math.Min(minY, bounds.MinY);
            maxX = Math.Max(maxX, bounds.MaxX);
            maxY = Math.Max(maxY, bounds.MaxY);
        }

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    private static double GetPolylineLength(PolylineEntity polyline)
    {
        PolylineEntity measurable = polyline.HasArcSegments
            ? polyline.ToPolylineApproximation()
            : polyline;
        double length = 0;

        for (int i = 1; i < measurable.Vertices.Count; i++)
        {
            length += measurable.Vertices[i - 1].DistanceTo(measurable.Vertices[i]);
        }

        if (measurable.IsClosed && measurable.Vertices.Count > 1)
        {
            length += measurable.Vertices[^1].DistanceTo(measurable.Vertices[0]);
        }

        return length;
    }

    private static double GetPolylineSignedArea(PolylineEntity polyline)
    {
        PolylineEntity measurable = polyline.HasArcSegments
            ? polyline.ToPolylineApproximation()
            : polyline;
        double sum = 0;

        for (int i = 0; i < measurable.Vertices.Count; i++)
        {
            Point2D current = measurable.Vertices[i];
            Point2D next = measurable.Vertices[(i + 1) % measurable.Vertices.Count];

            sum += current.X * next.Y - next.X * current.Y;
        }

        return sum / 2.0;
    }

    private static void ReplacePointCoordinate(CadWorkspace workspace, EntityId entityId, string value, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<PointEntity>(workspace, entityId, setMessage, out PointEntity point) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D replacementPoint = updateX
            ? new Point2D(coordinate, point.Position.Y)
            : new Point2D(point.Position.X, coordinate);

        ReplaceEntity(
            workspace,
            new PointEntity(replacementPoint, point.Id, point.LayerId, point.Style, point.IsVisible, point.IsLocked, point.DrawOrder),
            "Point updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceLineCoordinate(CadWorkspace workspace, EntityId entityId, string value, string pointName, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<LineEntity>(workspace, entityId, setMessage, out LineEntity line) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D start = line.Start;
        Point2D end = line.End;

        if (pointName == "start")
        {
            start = updateX ? new Point2D(coordinate, start.Y) : new Point2D(start.X, coordinate);
        }
        else
        {
            end = updateX ? new Point2D(coordinate, end.Y) : new Point2D(end.X, coordinate);
        }

        if (workspace.GeometryTolerance.ArePointsEqual(start, end))
        {
            setMessage?.Invoke("Line start and end points cannot be equal.");
            return;
        }

        ReplaceEntity(
            workspace,
            new LineEntity(start, end, line.Id, line.LayerId, line.Style, line.IsVisible, line.IsLocked, line.DrawOrder),
            "Line updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceCircleCoordinate(CadWorkspace workspace, EntityId entityId, string value, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<CircleEntity>(workspace, entityId, setMessage, out CircleEntity circle) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D center = updateX
            ? new Point2D(coordinate, circle.Center.Y)
            : new Point2D(circle.Center.X, coordinate);

        ReplaceEntity(
            workspace,
            new CircleEntity(center, circle.Radius, circle.Id, circle.LayerId, circle.Style, circle.IsVisible, circle.IsLocked, circle.DrawOrder, circle.IsFilled),
            "Circle updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceCircleRadius(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<CircleEntity>(workspace, entityId, setMessage, out CircleEntity circle) ||
            !TryParseDouble(value, setMessage, out double radius))
        {
            return;
        }

        if (radius <= 0)
        {
            setMessage?.Invoke("Circle radius must be greater than zero.");
            return;
        }

        ReplaceEntity(
            workspace,
            new CircleEntity(circle.Center, radius, circle.Id, circle.LayerId, circle.Style, circle.IsVisible, circle.IsLocked, circle.DrawOrder, circle.IsFilled),
            "Circle updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceCircleFill(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<CircleEntity>(workspace, entityId, setMessage, out CircleEntity circle) ||
            !TryParseFill(value, setMessage, out bool isFilled))
        {
            return;
        }

        ReplaceEntity(
            workspace,
            circle.WithFill(isFilled),
            "Circle fill updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceTextValue(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<TextEntity>(workspace, entityId, setMessage, out TextEntity text))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            setMessage?.Invoke("Text value cannot be empty.");
            return;
        }

        ReplaceEntity(workspace, text.WithText(value), "Text updated.", setMessage, refresh);
    }

    private static void ReplaceTextCoordinate(CadWorkspace workspace, EntityId entityId, string value, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<TextEntity>(workspace, entityId, setMessage, out TextEntity text) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D insertionPoint = updateX
            ? new Point2D(coordinate, text.InsertionPoint.Y)
            : new Point2D(text.InsertionPoint.X, coordinate);

        ReplaceEntity(workspace, text.WithInsertionPoint(insertionPoint), "Text updated.", setMessage, refresh);
    }

    private static void ReplaceTextRotation(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<TextEntity>(workspace, entityId, setMessage, out TextEntity text) ||
            !TryParseDouble(value, setMessage, out double rotationDegrees))
        {
            return;
        }

        ReplaceEntity(
            workspace,
            new TextEntity(text.InsertionPoint, text.Text, rotationDegrees, text.TextFormatId, text.Id, text.LayerId, text.Style, text.IsVisible, text.IsLocked, text.DrawOrder),
            "Text updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceTextFormat(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<TextEntity>(workspace, entityId, setMessage, out TextEntity text))
        {
            return;
        }

        var textFormatId = new TextFormatId(value.Trim());
        if (string.IsNullOrWhiteSpace(textFormatId.Value) || !workspace.Document.TextFormats.Contains(textFormatId))
        {
            setMessage?.Invoke("Text format was not found.");
            return;
        }

        ReplaceEntity(workspace, text.WithTextFormat(textFormatId), "Text format updated.", setMessage, refresh);
    }

    private static void ReplaceMultilineTextValue(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<MultilineTextEntity>(workspace, entityId, setMessage, out MultilineTextEntity text))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            setMessage?.Invoke("Multiline text value cannot be empty.");
            return;
        }

        ReplaceEntity(workspace, text.WithText(value), "Multiline text updated.", setMessage, refresh);
    }

    private static void ReplaceMultilineTextCoordinate(CadWorkspace workspace, EntityId entityId, string value, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<MultilineTextEntity>(workspace, entityId, setMessage, out MultilineTextEntity text) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D insertionPoint = updateX
            ? new Point2D(coordinate, text.InsertionPoint.Y)
            : new Point2D(text.InsertionPoint.X, coordinate);

        ReplaceEntity(workspace, text.WithInsertionPoint(insertionPoint), "Multiline text updated.", setMessage, refresh);
    }

    private static void ReplaceMultilineTextRotation(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<MultilineTextEntity>(workspace, entityId, setMessage, out MultilineTextEntity text) ||
            !TryParseDouble(value, setMessage, out double rotationDegrees))
        {
            return;
        }

        ReplaceEntity(
            workspace,
            new MultilineTextEntity(text.InsertionPoint, text.Text, rotationDegrees, text.TextFormatId, text.Id, text.LayerId, text.Style, text.IsVisible, text.IsLocked, text.DrawOrder, text.ReferenceWidth),
            "Multiline text updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceMultilineTextFormat(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<MultilineTextEntity>(workspace, entityId, setMessage, out MultilineTextEntity text))
        {
            return;
        }

        var textFormatId = new TextFormatId(value.Trim());
        if (string.IsNullOrWhiteSpace(textFormatId.Value) || !workspace.Document.TextFormats.Contains(textFormatId))
        {
            setMessage?.Invoke("Text format was not found.");
            return;
        }

        ReplaceEntity(workspace, text.WithTextFormat(textFormatId), "Multiline text format updated.", setMessage, refresh);
    }

    private static void ReplaceMultilineTextReferenceWidth(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<MultilineTextEntity>(workspace, entityId, setMessage, out MultilineTextEntity text) ||
            !TryParseDouble(value, setMessage, out double referenceWidth))
        {
            return;
        }

        if (referenceWidth < 0)
        {
            setMessage?.Invoke("Multiline text reference width cannot be negative.");
            return;
        }

        ReplaceEntity(workspace, text.WithReferenceWidth(referenceWidth), "Multiline text reference width updated.", setMessage, refresh);
    }


    private static void ReplaceImageReferenceFilePath(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ImageReferenceEntity>(workspace, entityId, setMessage, out ImageReferenceEntity imageReference))
        {
            return;
        }

        string filePath = value.Trim();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            setMessage?.Invoke("Image file path cannot be empty.");
            return;
        }

        ReplaceEntity(
            workspace,
            imageReference.WithFilePath(filePath, imageReference.PixelWidth, imageReference.PixelHeight),
            "Image reference path updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceImageReferenceOriginCoordinate(CadWorkspace workspace, EntityId entityId, string value, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ImageReferenceEntity>(workspace, entityId, setMessage, out ImageReferenceEntity imageReference) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D origin = updateX
            ? new Point2D(coordinate, imageReference.Origin.Y)
            : new Point2D(imageReference.Origin.X, coordinate);

        ReplaceEntity(
            workspace,
            imageReference.WithOrigin(origin),
            "Image reference origin updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceImageReferenceWidth(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ImageReferenceEntity>(workspace, entityId, setMessage, out ImageReferenceEntity imageReference) ||
            !TryParseDouble(value, setMessage, out double width))
        {
            return;
        }

        if (width <= 0)
        {
            setMessage?.Invoke("Image width must be greater than zero.");
            return;
        }

        ReplaceEntity(
            workspace,
            imageReference.WithSize(width, imageReference.Height),
            "Image reference width updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceImageReferenceHeight(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ImageReferenceEntity>(workspace, entityId, setMessage, out ImageReferenceEntity imageReference) ||
            !TryParseDouble(value, setMessage, out double height))
        {
            return;
        }

        if (height <= 0)
        {
            setMessage?.Invoke("Image height must be greater than zero.");
            return;
        }

        ReplaceEntity(
            workspace,
            imageReference.WithSize(imageReference.Width, height),
            "Image reference height updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceImageReferenceRotation(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ImageReferenceEntity>(workspace, entityId, setMessage, out ImageReferenceEntity imageReference) ||
            !TryParseDouble(value, setMessage, out double rotationDegrees))
        {
            return;
        }

        ReplaceEntity(
            workspace,
            imageReference.WithRotationDegrees(rotationDegrees),
            "Image reference rotation updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceImageReferenceTransparency(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ImageReferenceEntity>(workspace, entityId, setMessage, out ImageReferenceEntity imageReference) ||
            !TryParseDouble(value, setMessage, out double transparencyPercent))
        {
            return;
        }

        if (transparencyPercent < 0 || transparencyPercent > 100)
        {
            setMessage?.Invoke("Image transparency must be between 0 and 100 percent.");
            return;
        }

        ReplaceEntity(
            workspace,
            imageReference.WithTransparencyPercent(transparencyPercent),
            "Image reference transparency updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceArcCoordinate(CadWorkspace workspace, EntityId entityId, string value, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ArcEntity>(workspace, entityId, setMessage, out ArcEntity arc) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D center = updateX
            ? new Point2D(coordinate, arc.Center.Y)
            : new Point2D(arc.Center.X, coordinate);

        ReplaceEntity(
            workspace,
            new ArcEntity(center, arc.Radius, arc.StartAngle, arc.EndAngle, arc.IsCounterClockwise, arc.Id, arc.LayerId, arc.Style, arc.IsVisible, arc.IsLocked, arc.DrawOrder),
            "Arc updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceArcRadius(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ArcEntity>(workspace, entityId, setMessage, out ArcEntity arc) ||
            !TryParseDouble(value, setMessage, out double radius))
        {
            return;
        }

        if (radius <= 0)
        {
            setMessage?.Invoke("Arc radius must be greater than zero.");
            return;
        }

        ReplaceEntity(
            workspace,
            new ArcEntity(arc.Center, radius, arc.StartAngle, arc.EndAngle, arc.IsCounterClockwise, arc.Id, arc.LayerId, arc.Style, arc.IsVisible, arc.IsLocked, arc.DrawOrder),
            "Arc updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceArcAngle(CadWorkspace workspace, EntityId entityId, string value, bool updateStartAngle, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<ArcEntity>(workspace, entityId, setMessage, out ArcEntity arc) ||
            !TryParseDouble(value, setMessage, out double angleDegrees))
        {
            return;
        }

        Angle startAngle = updateStartAngle ? Angle.FromDegrees(angleDegrees) : arc.StartAngle;
        Angle endAngle = updateStartAngle ? arc.EndAngle : Angle.FromDegrees(angleDegrees);

        ReplaceEntity(
            workspace,
            new ArcEntity(arc.Center, arc.Radius, startAngle, endAngle, arc.IsCounterClockwise, arc.Id, arc.LayerId, arc.Style, arc.IsVisible, arc.IsLocked, arc.DrawOrder),
            "Arc updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceStairView(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParseStairView(value, setMessage, out StairViewKind viewKind))
        {
            return;
        }

        ReplaceEntity(workspace, stair.WithParameters(viewKind: viewKind), "Stair updated.", setMessage, refresh);
    }

    private static void ReplaceStairInsertionCoordinate(CadWorkspace workspace, EntityId entityId, string value, bool updateX, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D insertionPoint = updateX
            ? new Point2D(coordinate, stair.InsertionPoint.Y)
            : new Point2D(stair.InsertionPoint.X, coordinate);

        ReplaceEntity(
            workspace,
            RecreateStair(stair, insertionPoint: insertionPoint),
            "Stair updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceStairWidth(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParsePositiveDouble(value, "Stair width", setMessage, out double width))
        {
            return;
        }

        ReplaceEntity(workspace, stair.WithParameters(width: width), "Stair updated.", setMessage, refresh);
    }

    private static void ReplaceStairTreadCount(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParseInt(value, setMessage, out int treadCount))
        {
            return;
        }

        if (treadCount < 1)
        {
            setMessage?.Invoke("Stair tread count must be at least 1.");
            return;
        }

        ReplaceEntity(workspace, stair.WithParameters(treadCount: treadCount), "Stair updated.", setMessage, refresh);
    }

    private static void ReplaceStairTreadDepth(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParsePositiveDouble(value, "Stair tread depth", setMessage, out double treadDepth))
        {
            return;
        }

        ReplaceEntity(workspace, stair.WithParameters(treadDepth: treadDepth), "Stair updated.", setMessage, refresh);
    }

    private static void ReplaceStairRiserHeight(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParsePositiveDouble(value, "Stair riser height", setMessage, out double riserHeight))
        {
            return;
        }

        ReplaceEntity(workspace, stair.WithParameters(riserHeight: riserHeight), "Stair updated.", setMessage, refresh);
    }

    private static void ReplaceStairShowStructure(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParseBoolean(value, setMessage, out bool showStructure))
        {
            return;
        }

        ReplaceEntity(workspace, stair.WithParameters(showStructure: showStructure), "Stair updated.", setMessage, refresh);
    }

    private static void ReplaceStairSlabThickness(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<StairEntity>(workspace, entityId, setMessage, out StairEntity stair) ||
            !TryParseDouble(value, setMessage, out double slabThickness))
        {
            return;
        }

        if (slabThickness < 0.0)
        {
            setMessage?.Invoke("Stair slab thickness cannot be negative.");
            return;
        }

        ReplaceEntity(workspace, stair.WithParameters(slabThickness: slabThickness), "Stair updated.", setMessage, refresh);
    }

    private static StairEntity RecreateStair(StairEntity stair, Point2D? insertionPoint = null)
    {
        return new StairEntity(
            insertionPoint ?? stair.InsertionPoint,
            stair.ViewKind,
            stair.Width,
            stair.TreadCount,
            stair.TreadDepth,
            stair.RiserHeight,
            stair.ShowStructure,
            stair.SlabThickness,
            stair.XAxis,
            stair.YAxis,
            stair.Id,
            stair.LayerId,
            stair.Style,
            stair.IsVisible,
            stair.IsLocked,
            stair.DrawOrder);
    }

    private static void ReplacePolylineVertex(
        CadWorkspace workspace,
        EntityId entityId,
        int vertexIndex,
        string value,
        Action<string>? setMessage,
        Action? refresh)
    {
        if (!TryGetEditableEntity<PolylineEntity>(workspace, entityId, setMessage, out PolylineEntity polyline) ||
            !TryParseVertex(value, setMessage, out Point2D vertex))
        {
            return;
        }

        if (vertexIndex < 0 || vertexIndex >= polyline.Vertices.Count)
        {
            setMessage?.Invoke("Polyline vertex was not found.");
            return;
        }

        List<Point2D> vertices = polyline.Vertices.ToList();
        vertices[vertexIndex] = vertex;

        if (!ValidatePolylineVertices(vertices, polyline.IsClosed, setMessage))
        {
            return;
        }

        ReplaceEntity(
            workspace,
            new PolylineEntity(vertices, polyline.IsClosed, polyline.Id, polyline.LayerId, polyline.Style, polyline.IsVisible, polyline.IsLocked, polyline.DrawOrder, polyline.IsFilled, polyline.SegmentBulges),
            "Polyline vertex updated.",
            setMessage,
            refresh);
    }

    private static void ReplacePolylineSegmentBulge(
        CadWorkspace workspace,
        EntityId entityId,
        int segmentIndex,
        string value,
        Action<string>? setMessage,
        Action? refresh)
    {
        if (!TryGetEditableEntity<PolylineEntity>(workspace, entityId, setMessage, out PolylineEntity polyline) ||
            !TryParseDouble(value, setMessage, out double bulge))
        {
            return;
        }

        if (segmentIndex < 0 || segmentIndex >= polyline.SegmentBulges.Count)
        {
            setMessage?.Invoke("Polyline segment was not found.");
            return;
        }

        if (double.IsNaN(bulge) || double.IsInfinity(bulge))
        {
            setMessage?.Invoke("Polyline bulge must be a finite numeric value.");
            return;
        }

        List<double> bulges = polyline.SegmentBulges.ToList();
        bulges[segmentIndex] = bulge;

        ReplaceEntity(
            workspace,
            new PolylineEntity(polyline.Vertices, polyline.IsClosed, polyline.Id, polyline.LayerId, polyline.Style, polyline.IsVisible, polyline.IsLocked, polyline.DrawOrder, polyline.IsFilled, bulges),
            "Polyline segment bulge updated.",
            setMessage,
            refresh);
    }

    private static void ReplacePolylineClosed(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<PolylineEntity>(workspace, entityId, setMessage, out PolylineEntity polyline) ||
            !TryParseBoolean(value, setMessage, out bool isClosed))
        {
            return;
        }

        if (isClosed && polyline.Vertices.Count < 3)
        {
            setMessage?.Invoke("A closed polyline requires at least three vertices.");
            return;
        }

        ReplaceEntity(
            workspace,
            new PolylineEntity(polyline.Vertices, isClosed, polyline.Id, polyline.LayerId, polyline.Style, polyline.IsVisible, polyline.IsLocked, polyline.DrawOrder, isClosed && polyline.IsFilled, GetPolylineBulgesForClosedState(polyline, isClosed)),
            "Polyline updated.",
            setMessage,
            refresh);
    }


    private static IReadOnlyList<double> GetPolylineBulgesForClosedState(
        PolylineEntity polyline,
        bool isClosed)
    {
        if (polyline.IsClosed == isClosed)
        {
            return polyline.SegmentBulges;
        }

        if (isClosed)
        {
            return polyline.SegmentBulges.Concat(new[] { 0.0 }).ToList();
        }

        return polyline.SegmentBulges.Take(Math.Max(polyline.Vertices.Count - 1, 0)).ToList();
    }

    private static void ReplacePolylineFill(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<PolylineEntity>(workspace, entityId, setMessage, out PolylineEntity polyline) ||
            !TryParseFill(value, setMessage, out bool isFilled))
        {
            return;
        }

        if (!polyline.IsClosed)
        {
            setMessage?.Invoke("Only closed polylines can be filled.");
            return;
        }

        ReplaceEntity(
            workspace,
            polyline.WithFill(isFilled),
            "Polyline fill updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceEntityLayer(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        CadEntity entity;
        try
        {
            entity = workspace.Document.Entities.GetRequired(entityId);
        }
        catch (KeyNotFoundException)
        {
            setMessage?.Invoke("Selected entity was not found.");
            return;
        }

        var layerId = new LayerId(value.Trim());
        if (string.IsNullOrWhiteSpace(layerId.Value) || !workspace.Document.Layers.Contains(layerId))
        {
            setMessage?.Invoke("Layer was not found.");
            return;
        }

        ReplaceEntity(workspace, entity.WithLayer(layerId), "Entity layer updated.", setMessage, refresh);
    }

    private static bool ValidatePolylineVertices(
        IReadOnlyList<Point2D> vertices,
        bool isClosed,
        Action<string>? setMessage)
    {
        int minimumVertexCount = isClosed ? 3 : 2;

        if (vertices.Count < minimumVertexCount)
        {
            setMessage?.Invoke(isClosed
                ? "A closed polyline requires at least three vertices."
                : "A polyline requires at least two vertices.");
            return false;
        }

        for (int index = 1; index < vertices.Count; index++)
        {
            if (vertices[index - 1].DistanceTo(vertices[index]) <= 1e-9)
            {
                setMessage?.Invoke("Polyline vertices must not create zero-length consecutive segments.");
                return false;
            }
        }

        if (isClosed &&
            vertices.Count > 1 &&
            vertices[^1].DistanceTo(vertices[0]) <= 1e-9)
        {
            setMessage?.Invoke("Closed polyline first and last vertices must not be identical.");
            return false;
        }

        return true;
    }

    private static void ReplaceDimensionStyle(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<DimensionEntity>(workspace, entityId, setMessage, out DimensionEntity dimension))
        {
            return;
        }

        if (!TryResolveDimensionStyleId(workspace, value, out DimensionStyleId styleId))
        {
            setMessage?.Invoke("Dimension style was not found.");
            return;
        }

        ReplaceEntity(workspace, RecreateDimension(dimension, styleId, dimension.TextOverride), "Dimension style updated.", setMessage, refresh);
    }

    private static string GetDimensionStyleDisplayName(
        CadWorkspace workspace,
        DimensionStyleId styleId)
    {
        return workspace.Document.DimensionStyles.TryGetById(styleId, out DimensionStyle? style) &&
               style is not null
            ? style.Name
            : styleId.Value;
    }

    private static bool TryResolveDimensionStyleId(
        CadWorkspace workspace,
        string value,
        out DimensionStyleId styleId)
    {
        string trimmed = value.Trim();
        styleId = new DimensionStyleId(trimmed);

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        if (workspace.Document.DimensionStyles.Contains(styleId))
        {
            return true;
        }

        DimensionStyle? styleByName = workspace.Document.DimensionStyles.All
            .FirstOrDefault(style => string.Equals(
                style.Name,
                trimmed,
                StringComparison.OrdinalIgnoreCase));

        if (styleByName is null)
        {
            return false;
        }

        styleId = styleByName.Id;
        return true;
    }

    private static void ReplaceDimensionTextOverride(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<DimensionEntity>(workspace, entityId, setMessage, out DimensionEntity dimension))
        {
            return;
        }

        string? textOverride = string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "<automatic>", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();

        ReplaceEntity(workspace, RecreateDimension(dimension, dimension.DimensionStyleId, textOverride), "Dimension text override updated.", setMessage, refresh);
    }

    private static CadEntity RecreateDimension(
        DimensionEntity dimension,
        DimensionStyleId dimensionStyleId,
        string? textOverride)
    {
        return dimension switch
        {
            LinearDimensionEntity linear => new LinearDimensionEntity(linear.FirstPoint, linear.SecondPoint, linear.DimensionLinePoint, linear.Orientation, dimensionStyleId, textOverride, linear.Id, linear.LayerId, linear.Style, linear.IsVisible, linear.IsLocked, linear.DrawOrder, linear.IsStale),
            AlignedDimensionEntity aligned => new AlignedDimensionEntity(aligned.FirstPoint, aligned.SecondPoint, aligned.DimensionLinePoint, dimensionStyleId, textOverride, aligned.Id, aligned.LayerId, aligned.Style, aligned.IsVisible, aligned.IsLocked, aligned.DrawOrder, aligned.IsStale),
            RadiusDimensionEntity radius => new RadiusDimensionEntity(radius.Center, radius.PointOnCircle, radius.TextPoint, dimensionStyleId, textOverride, radius.Id, radius.LayerId, radius.Style, radius.IsVisible, radius.IsLocked, radius.DrawOrder, radius.IsStale),
            DiameterDimensionEntity diameter => new DiameterDimensionEntity(diameter.Center, diameter.PointOnCircle, diameter.TextPoint, dimensionStyleId, textOverride, diameter.Id, diameter.LayerId, diameter.Style, diameter.IsVisible, diameter.IsLocked, diameter.DrawOrder, diameter.IsStale),
            AngularDimensionEntity angular => new AngularDimensionEntity(angular.Center, angular.FirstRayPoint, angular.SecondRayPoint, angular.ArcPoint, angular.IsCounterClockwise, dimensionStyleId, textOverride, angular.Id, angular.LayerId, angular.Style, angular.IsVisible, angular.IsLocked, angular.DrawOrder, angular.IsStale),
            _ => throw new ArgumentException("Unsupported dimension entity.", nameof(dimension))
        };
    }

    private static bool TryGetEditableEntity<TEntity>(CadWorkspace workspace, EntityId entityId, Action<string>? setMessage, out TEntity entity)
        where TEntity : CadEntity
    {
        entity = null!;

        if (!workspace.Document.Entities.TryGet(entityId, out CadEntity? currentEntity) || currentEntity is not TEntity typedEntity)
        {
            setMessage?.Invoke("Selected entity was not found.");
            return false;
        }

        if (!workspace.Document.IsEntitySelectable(currentEntity))
        {
            setMessage?.Invoke("Selected entity cannot be edited because its layer is hidden or locked.");
            return false;
        }

        entity = typedEntity;
        return true;
    }

    private static IReadOnlyList<string> GetLayerIdOptions(CadWorkspace workspace)
    {
        return workspace.Document.Layers.All
            .Select(layer => layer.Id.Value)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatVertexValue(Point2D vertex)
    {
        return $"{PropertyValueFormatter.FormatCoordinate(vertex.X)}, {PropertyValueFormatter.FormatCoordinate(vertex.Y)}";
    }

    private static bool TryParseVertex(string value, Action<string>? setMessage, out Point2D vertex)
    {
        vertex = Point2D.Origin;

        string[] parts = value.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2 ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
        {
            setMessage?.Invoke("Invalid vertex value. Use X, Y with point as decimal separator, for example 10.5, 20.");
            return false;
        }

        vertex = new Point2D(x, y);
        return true;
    }

    private static bool TryParseDouble(string value, Action<string>? setMessage, out double result)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        setMessage?.Invoke("Invalid numeric value. Use point as decimal separator, for example 10.5.");
        return false;
    }

    private static bool TryParseInt(string value, Action<string>? setMessage, out int result)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        setMessage?.Invoke("Invalid whole-number value. Use an integer such as 12.");
        return false;
    }

    private static bool TryParsePositiveDouble(string value, string label, Action<string>? setMessage, out double result)
    {
        if (!TryParseDouble(value, setMessage, out result))
        {
            return false;
        }

        if (result <= 0.0)
        {
            setMessage?.Invoke($"{label} must be greater than zero.");
            return false;
        }

        return true;
    }

    private static string FormatStairView(StairViewKind viewKind)
    {
        return viewKind switch
        {
            StairViewKind.Plan => "Plan",
            StairViewKind.SideElevation => "Side elevation",
            StairViewKind.FrontElevation => "Front elevation",
            _ => viewKind.ToString()
        };
    }

    private static bool TryParseStairView(string value, Action<string>? setMessage, out StairViewKind viewKind)
    {
        string normalized = value.Trim().Replace(" ", string.Empty).Replace("-", string.Empty).ToLowerInvariant();

        switch (normalized)
        {
            case "plan":
                viewKind = StairViewKind.Plan;
                return true;
            case "side":
            case "sideelevation":
            case "section":
                viewKind = StairViewKind.SideElevation;
                return true;
            case "front":
            case "frontelevation":
                viewKind = StairViewKind.FrontElevation;
                return true;
            default:
                viewKind = StairViewKind.Plan;
                setMessage?.Invoke("Invalid stair view. Use Plan, Side elevation or Front elevation.");
                return false;
        }
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string FormatFill(bool isFilled)
    {
        return isFilled ? "Solid" : "None";
    }

    private static bool TryParseFill(string value, Action<string>? setMessage, out bool isFilled)
    {
        string normalized = value.Trim().ToLowerInvariant();

        if (normalized is "solid" or "yes" or "true" or "1")
        {
            isFilled = true;
            return true;
        }

        if (normalized is "none" or "no" or "false" or "0")
        {
            isFilled = false;
            return true;
        }

        isFilled = false;
        setMessage?.Invoke("Invalid fill value. Use None or Solid.");
        return false;
    }

    private static bool TryParseBoolean(string value, Action<string>? setMessage, out bool result)
    {
        string normalized = value.Trim().ToLowerInvariant();

        if (normalized is "true" or "yes" or "y" or "1" or "closed")
        {
            result = true;
            return true;
        }

        if (normalized is "false" or "no" or "n" or "0" or "open")
        {
            result = false;
            return true;
        }

        result = false;
        setMessage?.Invoke("Invalid boolean value. Use true/false, yes/no, open/closed, or 1/0.");
        return false;
    }

    private static void ReplaceEntity(CadWorkspace workspace, CadEntity replacement, string message, Action<string>? setMessage, Action? refresh)
    {
        workspace.CommandHistory.Execute(workspace.Document, new ReplaceEntitiesCommand(replacement));
        workspace.SelectionSet.ReplaceWith(replacement.Id);
        setMessage?.Invoke(message);
        refresh?.Invoke();
    }

    private static string GetEntityTypeName(CadEntity entity)
    {
        return entity switch
        {
            PointEntity => "Point",
            TextEntity => "Text",
            MultilineTextEntity => "Multiline Text",
            LinearDimensionEntity linearDimension => linearDimension.Orientation == DimensionOrientation.Horizontal ? "Horizontal Dimension" : "Vertical Dimension",
            AlignedDimensionEntity => "Aligned Dimension",
            RadiusDimensionEntity => "Radius Dimension",
            DiameterDimensionEntity => "Diameter Dimension",
            AngularDimensionEntity => "Angular Dimension",
            LineEntity => "Line",
            CircleEntity => "Circle",
            EllipseEntity => "Ellipse",
            EllipticalArcEntity => "Elliptical Arc",
            PolylineEntity => "Polyline",
            BezierSplineEntity => "Spline",
            ImageReferenceEntity => "Image Reference",
            StairEntity => "Stair",
            ArcEntity => "Arc",
            _ => entity.Kind.ToString()
        };
    }

    private static PropertyRowViewModel Row(string name, string value)
    {
        return new PropertyRowViewModel(name, value);
    }

    private static PropertyRowViewModel EditableRow(string name, string value, Action<string> apply)
    {
        return new PropertyRowViewModel(name, value, isEditable: true, apply);
    }

    private static PropertyRowViewModel ComboRow(
        string name,
        string value,
        IEnumerable<string> options,
        Action<string> apply)
    {
        return new PropertyRowViewModel(name, value, isEditable: true, apply, options);
    }
}
