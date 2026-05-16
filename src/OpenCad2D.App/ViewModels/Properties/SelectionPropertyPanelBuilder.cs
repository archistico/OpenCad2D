using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                    EditableRow(
                        "Layer id",
                        entity.LayerId.Value,
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
            LinearDimensionEntity linearDimension => BuildLinearDimensionGeometrySection(workspace, linearDimension, setMessage, refresh),
            AlignedDimensionEntity alignedDimension => BuildAlignedDimensionGeometrySection(workspace, alignedDimension, setMessage, refresh),
            RadiusDimensionEntity radiusDimension => BuildRadiusDimensionGeometrySection(workspace, radiusDimension, setMessage, refresh),
            DiameterDimensionEntity diameterDimension => BuildDiameterDimensionGeometrySection(workspace, diameterDimension, setMessage, refresh),
            AngularDimensionEntity angularDimension => BuildAngularDimensionGeometrySection(workspace, angularDimension, setMessage, refresh),
            LineEntity line => BuildLineGeometrySection(workspace, line, setMessage, refresh),
            CircleEntity circle => BuildCircleGeometrySection(workspace, circle, setMessage, refresh),
            PolylineEntity polyline => BuildPolylineGeometrySection(workspace, polyline, setMessage, refresh),
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
        rows.Add(EditableRow(
            "Dimension style",
            dimension.DimensionStyleId.Value,
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
                Row("Diameter", PropertyValueFormatter.FormatLength(diameter)),
                Row("Area", PropertyValueFormatter.FormatArea(area)),
                Row("Circumference", PropertyValueFormatter.FormatLength(circumference))
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
            EditableRow("Closed", PropertyValueFormatter.FormatBoolean(polyline.IsClosed), value => ReplacePolylineClosed(workspace, polyline.Id, value, setMessage, refresh)),
            Row("Length", PropertyValueFormatter.FormatLength(GetPolylineLength(polyline)))
        };

        if (polyline.IsClosed && polyline.Vertices.Count >= 3)
        {
            rows.Add(Row("Area", PropertyValueFormatter.FormatArea(Math.Abs(GetPolylineSignedArea(polyline)))));
        }

        return new PropertySectionViewModel(
            "Geometry",
            rows);
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
        double length = 0;

        for (int i = 1; i < polyline.Vertices.Count; i++)
        {
            length += polyline.Vertices[i - 1].DistanceTo(polyline.Vertices[i]);
        }

        if (polyline.IsClosed && polyline.Vertices.Count > 1)
        {
            length += polyline.Vertices[^1].DistanceTo(polyline.Vertices[0]);
        }

        return length;
    }

    private static double GetPolylineSignedArea(PolylineEntity polyline)
    {
        double sum = 0;

        for (int i = 0; i < polyline.Vertices.Count; i++)
        {
            Point2D current = polyline.Vertices[i];
            Point2D next = polyline.Vertices[(i + 1) % polyline.Vertices.Count];

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
            new CircleEntity(center, circle.Radius, circle.Id, circle.LayerId, circle.Style, circle.IsVisible, circle.IsLocked, circle.DrawOrder),
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
            new CircleEntity(circle.Center, radius, circle.Id, circle.LayerId, circle.Style, circle.IsVisible, circle.IsLocked, circle.DrawOrder),
            "Circle updated.",
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
            new PolylineEntity(polyline.Vertices, isClosed, polyline.Id, polyline.LayerId, polyline.Style, polyline.IsVisible, polyline.IsLocked, polyline.DrawOrder),
            "Polyline updated.",
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

    private static void ReplaceDimensionStyle(CadWorkspace workspace, EntityId entityId, string value, Action<string>? setMessage, Action? refresh)
    {
        if (!TryGetEditableEntity<DimensionEntity>(workspace, entityId, setMessage, out DimensionEntity dimension))
        {
            return;
        }

        var styleId = new DimensionStyleId(value.Trim());
        if (string.IsNullOrWhiteSpace(styleId.Value) || !workspace.Document.DimensionStyles.Contains(styleId))
        {
            setMessage?.Invoke("Dimension style was not found.");
            return;
        }

        ReplaceEntity(workspace, RecreateDimension(dimension, styleId, dimension.TextOverride), "Dimension style updated.", setMessage, refresh);
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
            LinearDimensionEntity linear => new LinearDimensionEntity(linear.FirstPoint, linear.SecondPoint, linear.DimensionLinePoint, linear.Orientation, dimensionStyleId, textOverride, linear.Id, linear.LayerId, linear.Style, linear.IsVisible, linear.IsLocked, linear.DrawOrder),
            AlignedDimensionEntity aligned => new AlignedDimensionEntity(aligned.FirstPoint, aligned.SecondPoint, aligned.DimensionLinePoint, dimensionStyleId, textOverride, aligned.Id, aligned.LayerId, aligned.Style, aligned.IsVisible, aligned.IsLocked, aligned.DrawOrder),
            RadiusDimensionEntity radius => new RadiusDimensionEntity(radius.Center, radius.PointOnCircle, radius.TextPoint, dimensionStyleId, textOverride, radius.Id, radius.LayerId, radius.Style, radius.IsVisible, radius.IsLocked, radius.DrawOrder),
            DiameterDimensionEntity diameter => new DiameterDimensionEntity(diameter.Center, diameter.PointOnCircle, diameter.TextPoint, dimensionStyleId, textOverride, diameter.Id, diameter.LayerId, diameter.Style, diameter.IsVisible, diameter.IsLocked, diameter.DrawOrder),
            AngularDimensionEntity angular => new AngularDimensionEntity(angular.Center, angular.FirstRayPoint, angular.SecondRayPoint, angular.ArcPoint, angular.IsCounterClockwise, dimensionStyleId, textOverride, angular.Id, angular.LayerId, angular.Style, angular.IsVisible, angular.IsLocked, angular.DrawOrder),
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

    private static bool TryParseDouble(string value, Action<string>? setMessage, out double result)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        setMessage?.Invoke("Invalid numeric value. Use point as decimal separator, for example 10.5.");
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
            LinearDimensionEntity linearDimension => linearDimension.Orientation == DimensionOrientation.Horizontal ? "Horizontal Dimension" : "Vertical Dimension",
            AlignedDimensionEntity => "Aligned Dimension",
            RadiusDimensionEntity => "Radius Dimension",
            DiameterDimensionEntity => "Diameter Dimension",
            AngularDimensionEntity => "Angular Dimension",
            LineEntity => "Line",
            CircleEntity => "Circle",
            PolylineEntity => "Polyline",
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
}
