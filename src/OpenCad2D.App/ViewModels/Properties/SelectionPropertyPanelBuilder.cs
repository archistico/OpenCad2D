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
                    Row("Entities", workspace.Document.Entities.Count.ToString()),
                    Row("Layers", workspace.Document.Layers.Count.ToString()),
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
                    Row("Id", entity.Id.ToString()),
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
            .Select(group => Row(group.Key, group.Count().ToString()))
            .ToList();

        var layerRows = selectedEntities
            .GroupBy(entity => workspace.Document.Layers.GetRequired(entity.LayerId).Name)
            .OrderBy(group => group.Key)
            .Select(group => Row(group.Key, group.Count().ToString()))
            .ToList();

        var sections = new List<PropertySectionViewModel>
        {
            new(
                "Selection",
                new[]
                {
                    Row("State", "Multiple selection"),
                    Row("Selected", selectedEntities.Count.ToString())
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
            LinearDimensionEntity linearDimension => BuildLinearDimensionGeometrySection(linearDimension),
            AlignedDimensionEntity alignedDimension => BuildAlignedDimensionGeometrySection(alignedDimension),
            RadiusDimensionEntity radiusDimension => BuildRadiusDimensionGeometrySection(radiusDimension),
            DiameterDimensionEntity diameterDimension => BuildDiameterDimensionGeometrySection(diameterDimension),
            AngularDimensionEntity angularDimension => BuildAngularDimensionGeometrySection(angularDimension),
            LineEntity line => BuildLineGeometrySection(workspace, line, setMessage, refresh),
            CircleEntity circle => BuildCircleGeometrySection(workspace, circle, setMessage, refresh),
            PolylineEntity polyline => BuildPolylineGeometrySection(polyline),
            ArcEntity arc => BuildArcGeometrySection(arc),
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
                    value => ReplacePointCoordinate(
                        workspace,
                        point.Id,
                        value,
                        updateX: true,
                        setMessage,
                        refresh)),
                EditableRow(
                    "Y",
                    PropertyValueFormatter.FormatCoordinate(point.Position.Y),
                    value => ReplacePointCoordinate(
                        workspace,
                        point.Id,
                        value,
                        updateX: false,
                        setMessage,
                        refresh))
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
                EditableRow(
                    "Value",
                    text.Text,
                    value => ReplaceTextValue(
                        workspace,
                        text.Id,
                        value,
                        setMessage,
                        refresh)),
                Row("Insertion", PropertyValueFormatter.FormatPoint(text.InsertionPoint)),
                EditableRow(
                    "X",
                    PropertyValueFormatter.FormatCoordinate(text.InsertionPoint.X),
                    value => ReplaceTextCoordinate(
                        workspace,
                        text.Id,
                        value,
                        updateX: true,
                        setMessage,
                        refresh)),
                EditableRow(
                    "Y",
                    PropertyValueFormatter.FormatCoordinate(text.InsertionPoint.Y),
                    value => ReplaceTextCoordinate(
                        workspace,
                        text.Id,
                        value,
                        updateX: false,
                        setMessage,
                        refresh)),
                EditableRow(
                    "Rotation",
                    PropertyValueFormatter.FormatCoordinate(text.RotationDegrees),
                    value => ReplaceTextRotation(
                        workspace,
                        text.Id,
                        value,
                        setMessage,
                        refresh)),
                Row("Format", text.TextFormatId.Value)
            });
    }


    private static PropertySectionViewModel BuildLinearDimensionGeometrySection(LinearDimensionEntity dimension)
    {
        return new PropertySectionViewModel(
            "Dimension",
            new[]
            {
                Row("Kind", dimension.Orientation == DimensionOrientation.Horizontal ? "Horizontal" : "Vertical"),
                Row("First point", PropertyValueFormatter.FormatPoint(dimension.FirstPoint)),
                Row("Second point", PropertyValueFormatter.FormatPoint(dimension.SecondPoint)),
                Row("Dimension line", PropertyValueFormatter.FormatPoint(dimension.DimensionLinePoint)),
                Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue)),
                Row("Style", dimension.DimensionStyleId.Value),
                Row("Text override", string.IsNullOrWhiteSpace(dimension.TextOverride) ? "<automatic>" : dimension.TextOverride!)
            });
    }

    private static PropertySectionViewModel BuildAlignedDimensionGeometrySection(AlignedDimensionEntity dimension)
    {
        return new PropertySectionViewModel(
            "Dimension",
            new[]
            {
                Row("Kind", "Aligned"),
                Row("First point", PropertyValueFormatter.FormatPoint(dimension.FirstPoint)),
                Row("Second point", PropertyValueFormatter.FormatPoint(dimension.SecondPoint)),
                Row("Dimension line", PropertyValueFormatter.FormatPoint(dimension.DimensionLinePoint)),
                Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue)),
                Row("Style", dimension.DimensionStyleId.Value),
                Row("Text override", string.IsNullOrWhiteSpace(dimension.TextOverride) ? "<automatic>" : dimension.TextOverride!)
            });
    }

    private static PropertySectionViewModel BuildRadiusDimensionGeometrySection(RadiusDimensionEntity dimension)
    {
        return new PropertySectionViewModel(
            "Dimension",
            new[]
            {
                Row("Kind", "Radius"),
                Row("Center", PropertyValueFormatter.FormatPoint(dimension.Center)),
                Row("Point on circle", PropertyValueFormatter.FormatPoint(dimension.PointOnCircle)),
                Row("Text point", PropertyValueFormatter.FormatPoint(dimension.TextPoint)),
                Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue)),
                Row("Style", dimension.DimensionStyleId.Value),
                Row("Text override", string.IsNullOrWhiteSpace(dimension.TextOverride) ? "<automatic>" : dimension.TextOverride!)
            });
    }

    private static PropertySectionViewModel BuildDiameterDimensionGeometrySection(DiameterDimensionEntity dimension)
    {
        return new PropertySectionViewModel(
            "Dimension",
            new[]
            {
                Row("Kind", "Diameter"),
                Row("Center", PropertyValueFormatter.FormatPoint(dimension.Center)),
                Row("Point on circle", PropertyValueFormatter.FormatPoint(dimension.PointOnCircle)),
                Row("Opposite point", PropertyValueFormatter.FormatPoint(dimension.OppositePoint)),
                Row("Text point", PropertyValueFormatter.FormatPoint(dimension.TextPoint)),
                Row("Measurement", PropertyValueFormatter.FormatLength(dimension.MeasurementValue)),
                Row("Style", dimension.DimensionStyleId.Value),
                Row("Text override", string.IsNullOrWhiteSpace(dimension.TextOverride) ? "<automatic>" : dimension.TextOverride!)
            });
    }

    private static PropertySectionViewModel BuildAngularDimensionGeometrySection(AngularDimensionEntity dimension)
    {
        return new PropertySectionViewModel(
            "Dimension",
            new[]
            {
                Row("Kind", "Angular"),
                Row("Center", PropertyValueFormatter.FormatPoint(dimension.Center)),
                Row("First ray", PropertyValueFormatter.FormatPoint(dimension.FirstRayPoint)),
                Row("Second ray", PropertyValueFormatter.FormatPoint(dimension.SecondRayPoint)),
                Row("Arc point", PropertyValueFormatter.FormatPoint(dimension.ArcPoint)),
                Row("Direction", dimension.IsCounterClockwise ? "Counter-clockwise" : "Clockwise"),
                Row("Measurement", PropertyValueFormatter.FormatAngleDegrees(dimension.MeasurementValue)),
                Row("Style", dimension.DimensionStyleId.Value),
                Row("Text override", string.IsNullOrWhiteSpace(dimension.TextOverride) ? "<automatic>" : dimension.TextOverride!)
            });
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
                EditableRow(
                    "Start X",
                    PropertyValueFormatter.FormatCoordinate(line.Start.X),
                    value => ReplaceLineCoordinate(
                        workspace,
                        line.Id,
                        value,
                        pointName: "start",
                        updateX: true,
                        setMessage,
                        refresh)),
                EditableRow(
                    "Start Y",
                    PropertyValueFormatter.FormatCoordinate(line.Start.Y),
                    value => ReplaceLineCoordinate(
                        workspace,
                        line.Id,
                        value,
                        pointName: "start",
                        updateX: false,
                        setMessage,
                        refresh)),
                Row("End", PropertyValueFormatter.FormatPoint(line.End)),
                EditableRow(
                    "End X",
                    PropertyValueFormatter.FormatCoordinate(line.End.X),
                    value => ReplaceLineCoordinate(
                        workspace,
                        line.Id,
                        value,
                        pointName: "end",
                        updateX: true,
                        setMessage,
                        refresh)),
                EditableRow(
                    "End Y",
                    PropertyValueFormatter.FormatCoordinate(line.End.Y),
                    value => ReplaceLineCoordinate(
                        workspace,
                        line.Id,
                        value,
                        pointName: "end",
                        updateX: false,
                        setMessage,
                        refresh)),
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
                EditableRow(
                    "Center X",
                    PropertyValueFormatter.FormatCoordinate(circle.Center.X),
                    value => ReplaceCircleCoordinate(
                        workspace,
                        circle.Id,
                        value,
                        updateX: true,
                        setMessage,
                        refresh)),
                EditableRow(
                    "Center Y",
                    PropertyValueFormatter.FormatCoordinate(circle.Center.Y),
                    value => ReplaceCircleCoordinate(
                        workspace,
                        circle.Id,
                        value,
                        updateX: false,
                        setMessage,
                        refresh)),
                EditableRow(
                    "Radius",
                    PropertyValueFormatter.FormatCoordinate(circle.Radius),
                    value => ReplaceCircleRadius(
                        workspace,
                        circle.Id,
                        value,
                        setMessage,
                        refresh)),
                Row("Diameter", PropertyValueFormatter.FormatLength(diameter)),
                Row("Area", PropertyValueFormatter.FormatArea(area)),
                Row("Circumference", PropertyValueFormatter.FormatLength(circumference))
            });
    }

    private static PropertySectionViewModel BuildPolylineGeometrySection(PolylineEntity polyline)
    {
        var rows = new List<PropertyRowViewModel>
        {
            Row("Vertices", polyline.Vertices.Count.ToString()),
            Row("Closed", PropertyValueFormatter.FormatBoolean(polyline.IsClosed)),
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

    private static PropertySectionViewModel BuildArcGeometrySection(ArcEntity arc)
    {
        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Center", PropertyValueFormatter.FormatPoint(arc.Center)),
                Row("Radius", PropertyValueFormatter.FormatLength(arc.Radius)),
                Row("Start angle", PropertyValueFormatter.FormatAngleDegrees(arc.StartAngle.Degrees)),
                Row("End angle", PropertyValueFormatter.FormatAngleDegrees(arc.EndAngle.Degrees))
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

        return new BoundingBox2D(
            minX,
            minY,
            maxX,
            maxY);
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


    private static void ReplacePointCoordinate(
        CadWorkspace workspace,
        EntityId entityId,
        string value,
        bool updateX,
        Action<string>? setMessage,
        Action? refresh)
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
            new PointEntity(
                replacementPoint,
                point.Id,
                point.LayerId,
                point.Style,
                point.IsVisible,
                point.IsLocked,
                point.DrawOrder),
            "Point updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceLineCoordinate(
        CadWorkspace workspace,
        EntityId entityId,
        string value,
        string pointName,
        bool updateX,
        Action<string>? setMessage,
        Action? refresh)
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
            start = updateX
                ? new Point2D(coordinate, start.Y)
                : new Point2D(start.X, coordinate);
        }
        else
        {
            end = updateX
                ? new Point2D(coordinate, end.Y)
                : new Point2D(end.X, coordinate);
        }

        if (workspace.GeometryTolerance.ArePointsEqual(start, end))
        {
            setMessage?.Invoke("Line start and end points cannot be equal.");
            return;
        }

        ReplaceEntity(
            workspace,
            new LineEntity(
                start,
                end,
                line.Id,
                line.LayerId,
                line.Style,
                line.IsVisible,
                line.IsLocked,
                line.DrawOrder),
            "Line updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceCircleCoordinate(
        CadWorkspace workspace,
        EntityId entityId,
        string value,
        bool updateX,
        Action<string>? setMessage,
        Action? refresh)
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
            new CircleEntity(
                center,
                circle.Radius,
                circle.Id,
                circle.LayerId,
                circle.Style,
                circle.IsVisible,
                circle.IsLocked,
                circle.DrawOrder),
            "Circle updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceCircleRadius(
        CadWorkspace workspace,
        EntityId entityId,
        string value,
        Action<string>? setMessage,
        Action? refresh)
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
            new CircleEntity(
                circle.Center,
                radius,
                circle.Id,
                circle.LayerId,
                circle.Style,
                circle.IsVisible,
                circle.IsLocked,
                circle.DrawOrder),
            "Circle updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceTextValue(
        CadWorkspace workspace,
        EntityId entityId,
        string value,
        Action<string>? setMessage,
        Action? refresh)
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

        ReplaceEntity(
            workspace,
            text.WithText(value),
            "Text updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceTextCoordinate(
        CadWorkspace workspace,
        EntityId entityId,
        string value,
        bool updateX,
        Action<string>? setMessage,
        Action? refresh)
    {
        if (!TryGetEditableEntity<TextEntity>(workspace, entityId, setMessage, out TextEntity text) ||
            !TryParseDouble(value, setMessage, out double coordinate))
        {
            return;
        }

        Point2D insertionPoint = updateX
            ? new Point2D(coordinate, text.InsertionPoint.Y)
            : new Point2D(text.InsertionPoint.X, coordinate);

        ReplaceEntity(
            workspace,
            text.WithInsertionPoint(insertionPoint),
            "Text updated.",
            setMessage,
            refresh);
    }

    private static void ReplaceTextRotation(
        CadWorkspace workspace,
        EntityId entityId,
        string value,
        Action<string>? setMessage,
        Action? refresh)
    {
        if (!TryGetEditableEntity<TextEntity>(workspace, entityId, setMessage, out TextEntity text) ||
            !TryParseDouble(value, setMessage, out double rotationDegrees))
        {
            return;
        }

        ReplaceEntity(
            workspace,
            new TextEntity(
                text.InsertionPoint,
                text.Text,
                rotationDegrees,
                text.TextFormatId,
                text.Id,
                text.LayerId,
                text.Style,
                text.IsVisible,
                text.IsLocked,
                text.DrawOrder),
            "Text updated.",
            setMessage,
            refresh);
    }

    private static bool TryGetEditableEntity<TEntity>(
        CadWorkspace workspace,
        EntityId entityId,
        Action<string>? setMessage,
        out TEntity entity)
        where TEntity : CadEntity
    {
        entity = null!;

        if (!workspace.Document.Entities.TryGet(entityId, out CadEntity? currentEntity) ||
            currentEntity is not TEntity typedEntity)
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

    private static bool TryParseDouble(
        string value,
        Action<string>? setMessage,
        out double result)
    {
        if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result))
        {
            return true;
        }

        setMessage?.Invoke("Invalid numeric value. Use point as decimal separator, for example 10.5.");
        return false;
    }

    private static void ReplaceEntity(
        CadWorkspace workspace,
        CadEntity replacement,
        string message,
        Action<string>? setMessage,
        Action? refresh)
    {
        workspace.CommandHistory.Execute(
            workspace.Document,
            new ReplaceEntitiesCommand(replacement));

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
            LinearDimensionEntity linearDimension => linearDimension.Orientation == DimensionOrientation.Horizontal
                ? "Horizontal Dimension"
                : "Vertical Dimension",
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

    private static PropertyRowViewModel Row(
        string name,
        string value)
    {
        return new PropertyRowViewModel(
            name,
            value);
    }

    private static PropertyRowViewModel EditableRow(
        string name,
        string value,
        Action<string> apply)
    {
        return new PropertyRowViewModel(
            name,
            value,
            isEditable: true,
            apply);
    }
}
