using System;
using System.Collections.Generic;
using System.Linq;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.App.ViewModels.Properties;

public sealed class SelectionPropertyPanelBuilder
{
    public PropertyPanelViewModel Build(
        CadWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        IReadOnlyList<CadEntity> selectedEntities = workspace.SelectionSet.SelectedIds
            .Where(workspace.Document.Entities.Contains)
            .Select(workspace.Document.Entities.GetRequired)
            .ToList();

        return selectedEntities.Count switch
        {
            0 => BuildNoSelectionPanel(workspace),
            1 => BuildSingleSelectionPanel(workspace, selectedEntities[0]),
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
        CadEntity entity)
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

        sections.Add(BuildGeometrySection(entity));
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

    private static PropertySectionViewModel BuildGeometrySection(CadEntity entity)
    {
        return entity switch
        {
            LineEntity line => BuildLineGeometrySection(line),
            CircleEntity circle => BuildCircleGeometrySection(circle),
            PolylineEntity polyline => BuildPolylineGeometrySection(polyline),
            ArcEntity arc => BuildArcGeometrySection(arc),
            _ => new PropertySectionViewModel(
                "Geometry",
                new[] { Row("Details", "Not available") })
        };
    }

    private static PropertySectionViewModel BuildLineGeometrySection(LineEntity line)
    {
        Vector2D delta = line.Start.VectorTo(line.End);
        double angleDegrees = Math.Atan2(delta.Y, delta.X) * 180.0 / Math.PI;

        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Start", PropertyValueFormatter.FormatPoint(line.Start)),
                Row("End", PropertyValueFormatter.FormatPoint(line.End)),
                Row("Length", PropertyValueFormatter.FormatLength(delta.Length)),
                Row("DX", PropertyValueFormatter.FormatLength(delta.X)),
                Row("DY", PropertyValueFormatter.FormatLength(delta.Y)),
                Row("Angle", PropertyValueFormatter.FormatAngleDegrees(angleDegrees))
            });
    }

    private static PropertySectionViewModel BuildCircleGeometrySection(CircleEntity circle)
    {
        double diameter = circle.Radius * 2.0;
        double area = Math.PI * circle.Radius * circle.Radius;
        double circumference = 2.0 * Math.PI * circle.Radius;

        return new PropertySectionViewModel(
            "Geometry",
            new[]
            {
                Row("Center", PropertyValueFormatter.FormatPoint(circle.Center)),
                Row("Radius", PropertyValueFormatter.FormatLength(circle.Radius)),
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

    private static string GetEntityTypeName(CadEntity entity)
    {
        return entity switch
        {
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
}
