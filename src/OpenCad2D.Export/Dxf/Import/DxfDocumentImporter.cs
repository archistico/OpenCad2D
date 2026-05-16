using System.Globalization;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Imports the supported subset of ASCII DXF into an OpenCad2D document.
/// </summary>
public sealed class DxfDocumentImporter : IDxfImporter
{
    private readonly DxfReader _reader;
    private readonly DxfSectionReader _sectionReader;

    public DxfDocumentImporter()
        : this(
            new DxfReader(),
            new DxfSectionReader())
    {
    }

    public DxfDocumentImporter(
        DxfReader reader,
        DxfSectionReader sectionReader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _sectionReader = sectionReader ?? throw new ArgumentNullException(nameof(sectionReader));
    }

    /// <inheritdoc />
    public DxfImportResult Import(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            IReadOnlyList<DxfCodePair> pairs = _reader.Read(content);

            return ImportPairs(pairs);
        }
        catch (DxfReadException exception)
        {
            return CreateFailedResult(exception.Message);
        }
    }

    /// <inheritdoc />
    public DxfImportResult ImportFile(string filePath)
    {
        try
        {
            IReadOnlyList<DxfCodePair> pairs = _reader.ReadFile(filePath);

            return ImportPairs(pairs);
        }
        catch (DxfReadException exception)
        {
            return CreateFailedResult(exception.Message);
        }
    }

    private DxfImportResult ImportPairs(IReadOnlyList<DxfCodePair> pairs)
    {
        var document = new CadDocument();
        var log = new List<DxfImportLogEntry>();
        IReadOnlyList<DxfSection> sections;

        try
        {
            sections = _sectionReader.ReadSections(pairs);
        }
        catch (DxfReadException exception)
        {
            return CreateFailedResult(exception.Message);
        }

        DxfSection? tablesSection = sections.FirstOrDefault(
            section => section.Name == "TABLES");

        if (tablesSection is not null)
        {
            ImportLayerTable(
                tablesSection,
                document,
                log);
        }

        DxfSection? entitiesSection = sections.FirstOrDefault(
            section => section.Name == "ENTITIES");

        if (entitiesSection is null)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "DXF file does not contain an ENTITIES section. An empty document was created."));

            return new DxfImportResult(
                document,
                log);
        }

        ImportEntities(
            entitiesSection,
            document,
            log);

        return new DxfImportResult(
            document,
            log);
    }

    private static DxfImportResult CreateFailedResult(string message)
    {
        var log = new List<DxfImportLogEntry>
        {
            new(
                DxfImportLogSeverity.Error,
                message)
        };

        return new DxfImportResult(
            new CadDocument(),
            log);
    }

    private static void ImportLayerTable(
        DxfSection tablesSection,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        foreach (DxfTable table in ReadTables(tablesSection.Pairs))
        {
            if (table.Name != "LAYER")
            {
                continue;
            }

            foreach (DxfTableRecord record in ReadTableRecords(table.Pairs))
            {
                if (record.Type != "LAYER")
                {
                    continue;
                }

                ImportLayerRecord(
                    record,
                    document,
                    log);
            }
        }
    }

    private static IReadOnlyList<DxfTable> ReadTables(IReadOnlyList<DxfCodePair> pairs)
    {
        var tables = new List<DxfTable>();
        int index = 0;

        while (index < pairs.Count)
        {
            DxfCodePair current = pairs[index];

            if (!current.IsMarkerValue("TABLE"))
            {
                index++;
                continue;
            }

            int startLineNumber = current.CodeLineNumber;
            index++;

            if (index >= pairs.Count || pairs[index].Code != 2)
            {
                index++;
                continue;
            }

            string tableName = pairs[index].Value.Trim().ToUpperInvariant();
            index++;

            var tablePairs = new List<DxfCodePair>();

            while (index < pairs.Count && !pairs[index].IsMarkerValue("ENDTAB"))
            {
                tablePairs.Add(pairs[index]);
                index++;
            }

            if (index < pairs.Count && pairs[index].IsMarkerValue("ENDTAB"))
            {
                index++;
            }

            tables.Add(new DxfTable(
                tableName,
                tablePairs,
                startLineNumber));
        }

        return tables;
    }

    private static IReadOnlyList<DxfTableRecord> ReadTableRecords(IReadOnlyList<DxfCodePair> pairs)
    {
        var records = new List<DxfTableRecord>();
        int index = 0;

        while (index < pairs.Count)
        {
            DxfCodePair current = pairs[index];

            if (!current.IsMarker)
            {
                index++;
                continue;
            }

            string recordType = current.Value.Trim().ToUpperInvariant();
            int startLineNumber = current.CodeLineNumber;
            index++;

            var recordPairs = new List<DxfCodePair>();

            while (index < pairs.Count && !pairs[index].IsMarker)
            {
                recordPairs.Add(pairs[index]);
                index++;
            }

            records.Add(new DxfTableRecord(
                recordType,
                recordPairs,
                startLineNumber));
        }

        return records;
    }

    private static void ImportLayerRecord(
        DxfTableRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        DxfCodePair? namePair = record.LastOrDefault(2);

        if (namePair is null || string.IsNullOrWhiteSpace(namePair.Value.Value))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped DXF LAYER table record because the layer name is missing or empty.",
                record.StartLineNumber));

            return;
        }

        string layerName = namePair.Value.Value.Trim();
        LayerId layerId = new(layerName);
        int flags = ReadOptionalInt(
            record,
            code: 70,
            defaultValue: 0);
        int aciColor = ReadOptionalInt(
            record,
            code: 62,
            defaultValue: 7);
        int lineWeightValue = ReadOptionalInt(
            record,
            code: 370,
            defaultValue: -1);

        bool isFrozen = (flags & 1) == 1;
        bool isLocked = (flags & 4) == 4;
        bool isVisible = aciColor >= 0 && !isFrozen;
        CadColor color = FromAci(Math.Abs(aciColor));
        LineWeight lineWeight = FromDxfLineWeight(lineWeightValue);
        DxfCodePair? lineTypePair = record.LastOrDefault(6);
        LineFormatId lineFormatId = ToLineFormatId(lineTypePair is null
            ? null
            : lineTypePair.Value.Value);

        var layer = new Layer(
            layerId,
            layerName,
            color,
            lineWeight,
            isVisible,
            isLocked).WithLineFormat(lineFormatId);

        if (document.Layers.Contains(layerId))
        {
            document.Layers.Replace(layer);
            return;
        }

        document.Layers.Add(layer);
    }

    private static void ImportEntities(
        DxfSection entitiesSection,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        foreach (DxfEntityRecord entityRecord in ReadEntityRecords(entitiesSection.Pairs))
        {
            if (entityRecord.Type == "LINE")
            {
                ImportLine(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "CIRCLE")
            {
                ImportCircle(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "POINT")
            {
                ImportPoint(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "ARC")
            {
                ImportArc(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "LWPOLYLINE")
            {
                ImportLightweightPolyline(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "TEXT")
            {
                ImportText(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "MTEXT")
            {
                ImportMultilineText(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "ELLIPSE")
            {
                ImportEllipse(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            if (entityRecord.Type == "SPLINE")
            {
                ImportSpline(
                    entityRecord,
                    document,
                    log);

                continue;
            }

            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                $"Skipped unsupported DXF entity: {entityRecord.Type}.",
                entityRecord.StartLineNumber));
        }
    }

    private static IReadOnlyList<DxfEntityRecord> ReadEntityRecords(IReadOnlyList<DxfCodePair> pairs)
    {
        var records = new List<DxfEntityRecord>();
        int index = 0;

        while (index < pairs.Count)
        {
            DxfCodePair current = pairs[index];

            if (!current.IsMarker)
            {
                index++;
                continue;
            }

            string entityType = current.Value.Trim().ToUpperInvariant();
            int startLineNumber = current.CodeLineNumber;
            index++;

            var entityPairs = new List<DxfCodePair>();

            while (index < pairs.Count && !pairs[index].IsMarker)
            {
                entityPairs.Add(pairs[index]);
                index++;
            }

            records.Add(new DxfEntityRecord(
                entityType,
                entityPairs,
                startLineNumber));
        }

        return records;
    }

    private static void ImportLine(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        if (!TryReadPoint(
                record,
                xCode: 10,
                yCode: 20,
                pointName: "start point",
                log,
                out Point2D start) ||
            !TryReadPoint(
                record,
                xCode: 11,
                yCode: 21,
                pointName: "end point",
                log,
                out Point2D end))
        {
            return;
        }

        if (Tolerance.ArePointsEqual(start, end))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped LINE entity because its start point and end point are equal.",
                record.StartLineNumber));

            return;
        }

        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        document.AddEntity(new LineEntity(
            start,
            end,
            layerId: layerId));
    }

    private static void ImportCircle(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        if (!TryReadPoint(
                record,
                xCode: 10,
                yCode: 20,
                pointName: "center point",
                log,
                out Point2D center) ||
            !TryReadDouble(
                record,
                code: 40,
                fieldName: "CIRCLE radius",
                log,
                out double radius))
        {
            return;
        }

        if (radius <= 0)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped CIRCLE entity because its radius is less than or equal to zero.",
                record.StartLineNumber));

            return;
        }

        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        document.AddEntity(new CircleEntity(
            center,
            radius,
            layerId: layerId));
    }

    private static void ImportPoint(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        if (!TryReadPoint(
                record,
                xCode: 10,
                yCode: 20,
                pointName: "position",
                log,
                out Point2D position))
        {
            return;
        }

        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        document.AddEntity(new PointEntity(
            position,
            layerId: layerId));
    }


    private static void ImportArc(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        if (!TryReadPoint(
                record,
                xCode: 10,
                yCode: 20,
                pointName: "center point",
                log,
                out Point2D center) ||
            !TryReadDouble(
                record,
                code: 40,
                fieldName: "ARC radius",
                log,
                out double radius) ||
            !TryReadDouble(
                record,
                code: 50,
                fieldName: "ARC start angle",
                log,
                out double startAngleDegrees) ||
            !TryReadDouble(
                record,
                code: 51,
                fieldName: "ARC end angle",
                log,
                out double endAngleDegrees))
        {
            return;
        }

        if (radius <= 0)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped ARC entity because its radius is less than or equal to zero.",
                record.StartLineNumber));

            return;
        }

        if (AreEquivalentAngles(
                startAngleDegrees,
                endAngleDegrees))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped ARC entity because its start angle and end angle are equal.",
                record.StartLineNumber));

            return;
        }

        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        document.AddEntity(new ArcEntity(
            center,
            radius,
            Angle.FromDegrees(startAngleDegrees),
            Angle.FromDegrees(endAngleDegrees),
            isCounterClockwise: true,
            layerId: layerId));
    }

    private static void ImportLightweightPolyline(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        int initialLogCount = log.Count;

        List<DxfPolylineVertex> vertices = ReadLightweightPolylineVertices(
            record,
            log);

        if (vertices.Count < 2)
        {
            if (log.Count == initialLogCount)
            {
                log.Add(new DxfImportLogEntry(
                    DxfImportLogSeverity.Warning,
                    "Skipped LWPOLYLINE entity because it contains fewer than two valid vertices.",
                    record.StartLineNumber));
            }

            return;
        }

        bool isClosed = IsLightweightPolylineClosed(record);
        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        bool hasBulge = vertices.Any(vertex =>
            !Tolerance.AreEqual(vertex.Bulge, 0));

        if (hasBulge)
        {
            ImportLightweightPolylineWithBulges(
                record,
                vertices,
                isClosed,
                layerId,
                document,
                log);

            return;
        }

        document.AddEntity(new PolylineEntity(
            vertices.Select(vertex => vertex.Point),
            isClosed,
            layerId: layerId));
    }


    private static void ImportLightweightPolylineWithBulges(
        DxfEntityRecord record,
        IReadOnlyList<DxfPolylineVertex> vertices,
        bool isClosed,
        LayerId layerId,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        int segmentCount = isClosed
            ? vertices.Count
            : vertices.Count - 1;

        int importedSegmentCount = 0;

        for (int index = 0; index < segmentCount; index++)
        {
            DxfPolylineVertex current = vertices[index];
            DxfPolylineVertex next = vertices[(index + 1) % vertices.Count];

            if (Tolerance.ArePointsEqual(current.Point, next.Point))
            {
                log.Add(new DxfImportLogEntry(
                    DxfImportLogSeverity.Warning,
                    "Skipped zero-length LWPOLYLINE segment while converting bulge geometry.",
                    record.StartLineNumber));

                continue;
            }

            if (Tolerance.AreEqual(current.Bulge, 0))
            {
                document.AddEntity(new LineEntity(
                    current.Point,
                    next.Point,
                    layerId: layerId));
                importedSegmentCount++;
                continue;
            }

            if (TryCreateArcFromBulge(
                    current.Point,
                    next.Point,
                    current.Bulge,
                    out ArcEntity? arc)
                && arc is not null)
            {
                document.AddEntity(new ArcEntity(
                    arc.Center,
                    arc.Radius,
                    arc.StartAngle,
                    arc.EndAngle,
                    arc.IsCounterClockwise,
                    layerId: layerId));
                importedSegmentCount++;
                continue;
            }

            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped invalid LWPOLYLINE bulge segment because it could not be converted to an arc.",
                record.StartLineNumber));
        }

        if (importedSegmentCount == 0)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped LWPOLYLINE entity because no valid bulge segments could be imported.",
                record.StartLineNumber));
            return;
        }

        log.Add(new DxfImportLogEntry(
            DxfImportLogSeverity.Info,
            "Imported LWPOLYLINE bulge geometry as separate line and arc entities.",
            record.StartLineNumber));
    }

    private static bool TryCreateArcFromBulge(
        Point2D start,
        Point2D end,
        double bulge,
        out ArcEntity? arc)
    {
        arc = null;

        if (Tolerance.AreEqual(bulge, 0) ||
            Tolerance.ArePointsEqual(start, end))
        {
            return false;
        }

        double chordLength = start.DistanceTo(end);
        double radius = chordLength * (1 + (bulge * bulge)) / (4 * Math.Abs(bulge));

        if (radius <= 0 || double.IsNaN(radius) || double.IsInfinity(radius))
        {
            return false;
        }

        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double leftNormalX = -dy / chordLength;
        double leftNormalY = dx / chordLength;
        double centerOffset = chordLength * (1 - (bulge * bulge)) / (4 * bulge);

        Point2D midpoint = new(
            (start.X + end.X) / 2.0,
            (start.Y + end.Y) / 2.0);

        Point2D center = new(
            midpoint.X + (leftNormalX * centerOffset),
            midpoint.Y + (leftNormalY * centerOffset));

        Angle startAngle = Angle.FromRadians(
            Math.Atan2(
                start.Y - center.Y,
                start.X - center.X));

        Angle endAngle = Angle.FromRadians(
            Math.Atan2(
                end.Y - center.Y,
                end.X - center.X));

        arc = new ArcEntity(
            center,
            radius,
            startAngle,
            endAngle,
            isCounterClockwise: bulge > 0);

        return true;
    }

    private static List<DxfPolylineVertex> ReadLightweightPolylineVertices(
        DxfEntityRecord record,
        List<DxfImportLogEntry> log)
    {
        var vertices = new List<DxfPolylineVertex>();
        double? currentX = null;
        int currentXLineNumber = record.StartLineNumber;

        foreach (DxfCodePair pair in record.Pairs)
        {
            if (pair.Code == 10)
            {
                if (currentX.HasValue)
                {
                    log.Add(new DxfImportLogEntry(
                        DxfImportLogSeverity.Warning,
                        "Skipped LWPOLYLINE entity because a vertex X coordinate is missing its matching Y coordinate.",
                        currentXLineNumber));

                    return new List<DxfPolylineVertex>();
                }

                if (!TryParseDouble(
                        pair,
                        record,
                        "LWPOLYLINE vertex X coordinate",
                        log,
                        out double x))
                {
                    return new List<DxfPolylineVertex>();
                }

                currentX = x;
                currentXLineNumber = pair.CodeLineNumber;
                continue;
            }

            if (pair.Code == 20)
            {
                if (!currentX.HasValue)
                {
                    log.Add(new DxfImportLogEntry(
                        DxfImportLogSeverity.Warning,
                        "Skipped LWPOLYLINE entity because a vertex Y coordinate appears before its X coordinate.",
                        pair.CodeLineNumber));

                    return new List<DxfPolylineVertex>();
                }

                if (!TryParseDouble(
                        pair,
                        record,
                        "LWPOLYLINE vertex Y coordinate",
                        log,
                        out double y))
                {
                    return new List<DxfPolylineVertex>();
                }

                vertices.Add(new DxfPolylineVertex(
                    new Point2D(currentX.Value, y),
                    0));

                currentX = null;
                continue;
            }

            if (pair.Code == 42)
            {
                if (vertices.Count == 0)
                {
                    log.Add(new DxfImportLogEntry(
                        DxfImportLogSeverity.Warning,
                        "Skipped LWPOLYLINE entity because a bulge value appears before the first vertex.",
                        pair.CodeLineNumber));

                    return new List<DxfPolylineVertex>();
                }

                if (!TryParseDouble(
                        pair,
                        record,
                        "LWPOLYLINE bulge",
                        log,
                        out double bulge))
                {
                    return new List<DxfPolylineVertex>();
                }

                DxfPolylineVertex lastVertex = vertices[^1];
                vertices[^1] = lastVertex with { Bulge = bulge };
            }
        }

        if (currentX.HasValue)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped LWPOLYLINE entity because a vertex X coordinate is missing its matching Y coordinate.",
                currentXLineNumber));

            return new List<DxfPolylineVertex>();
        }

        return vertices;
    }

    private static bool IsLightweightPolylineClosed(DxfEntityRecord record)
    {
        DxfCodePair? flagsPair = record.LastOrDefault(70);

        if (flagsPair is null)
        {
            return false;
        }

        if (!int.TryParse(
                flagsPair.Value.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int flags))
        {
            return false;
        }

        return (flags & 1) == 1;
    }

    private static void ImportEllipse(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        if (!TryReadPoint(
                record,
                xCode: 10,
                yCode: 20,
                pointName: "center point",
                log,
                out Point2D center) ||
            !TryReadDouble(
                record,
                code: 11,
                fieldName: "ELLIPSE major axis X coordinate",
                log,
                out double majorAxisX) ||
            !TryReadDouble(
                record,
                code: 21,
                fieldName: "ELLIPSE major axis Y coordinate",
                log,
                out double majorAxisY) ||
            !TryReadDouble(
                record,
                code: 40,
                fieldName: "ELLIPSE minor-to-major axis ratio",
                log,
                out double axisRatio))
        {
            return;
        }

        var majorAxis = new Vector2D(majorAxisX, majorAxisY);

        if (majorAxis.Length <= Tolerance.Default)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped ELLIPSE entity because its major axis vector is zero-length.",
                record.StartLineNumber));

            return;
        }

        if (axisRatio <= 0 || double.IsNaN(axisRatio) || double.IsInfinity(axisRatio))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped ELLIPSE entity because its minor-to-major axis ratio is invalid.",
                record.StartLineNumber));

            return;
        }

        double minorRadius = majorAxis.Length * axisRatio;

        if (minorRadius <= Tolerance.Default)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped ELLIPSE entity because its minor radius is zero-length.",
                record.StartLineNumber));

            return;
        }

        double startParameter = 0.0;
        double endParameter = Math.Tau;

        if (!TryReadOptionalDouble(
                record,
                code: 41,
                fieldName: "ELLIPSE start parameter",
                log,
                out double? parsedStartParameter) ||
            !TryReadOptionalDouble(
                record,
                code: 42,
                fieldName: "ELLIPSE end parameter",
                log,
                out double? parsedEndParameter))
        {
            return;
        }

        if (parsedStartParameter.HasValue)
        {
            startParameter = parsedStartParameter.Value;
        }

        if (parsedEndParameter.HasValue)
        {
            endParameter = parsedEndParameter.Value;
        }

        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        if (IsFullEllipseParameterRange(
                startParameter,
                endParameter))
        {
            document.AddEntity(new EllipseEntity(
                center,
                majorAxis,
                minorRadius,
                layerId: layerId));

            return;
        }

        IReadOnlyList<Point2D> points = SampleEllipseArc(
            center,
            majorAxis,
            minorRadius,
            startParameter,
            endParameter);

        if (points.Count < 2)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped ELLIPSE entity because its parameter range is too small to import.",
                record.StartLineNumber));

            return;
        }

        document.AddEntity(new PolylineEntity(
            points,
            isClosed: false,
            layerId: layerId));

        log.Add(new DxfImportLogEntry(
            DxfImportLogSeverity.Info,
            "Imported partial ELLIPSE entity as an approximated open polyline.",
            record.StartLineNumber));
    }

    private static bool IsFullEllipseParameterRange(
        double startParameter,
        double endParameter)
    {
        double span = NormalizePositiveRadians(endParameter - startParameter);

        return Math.Abs(span) <= Tolerance.Default ||
            Math.Abs(span - Math.Tau) <= Tolerance.Default;
    }

    private static IReadOnlyList<Point2D> SampleEllipseArc(
        Point2D center,
        Vector2D majorAxis,
        double minorRadius,
        double startParameter,
        double endParameter)
    {
        double span = NormalizePositiveRadians(endParameter - startParameter);

        if (span <= Tolerance.Default)
        {
            span = Math.Tau;
        }

        int segmentCount = Math.Clamp(
            (int)Math.Ceiling(64.0 * span / Math.Tau),
            8,
            96);

        Vector2D minorAxis = majorAxis.Normalize().PerpendicularLeft() * minorRadius;
        var points = new List<Point2D>(segmentCount + 1);

        for (int index = 0; index <= segmentCount; index++)
        {
            double parameter = startParameter + (span * index / segmentCount);
            points.Add(center +
                majorAxis * Math.Cos(parameter) +
                minorAxis * Math.Sin(parameter));
        }

        return points;
    }

    private static double NormalizePositiveRadians(double radians)
    {
        double normalized = radians % Math.Tau;

        if (normalized < 0)
        {
            normalized += Math.Tau;
        }

        return normalized;
    }

    private static void ImportSpline(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        IReadOnlyList<Point2D> controlPoints = ReadSplinePointSequence(
            record,
            xCode: 10,
            yCode: 20,
            pointKind: "control point",
            log);

        if (controlPoints.Count >= 2)
        {
            LayerId layerId = EnsureLayer(
                document,
                GetLayerName(record));

            bool isClosed = IsSplineClosed(record);

            document.AddEntity(new BezierSplineEntity(
                controlPoints,
                isClosed,
                layerId: layerId));

            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Info,
                "Imported DXF SPLINE control points as a BezierSplineEntity approximation.",
                record.StartLineNumber));

            return;
        }

        IReadOnlyList<Point2D> fitPoints = ReadSplinePointSequence(
            record,
            xCode: 11,
            yCode: 21,
            pointKind: "fit point",
            log);

        if (fitPoints.Count >= 2)
        {
            LayerId layerId = EnsureLayer(
                document,
                GetLayerName(record));

            document.AddEntity(new PolylineEntity(
                fitPoints,
                IsSplineClosed(record),
                layerId: layerId));

            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Info,
                "Imported DXF SPLINE fit points as an approximated polyline because OpenCad2D does not yet evaluate external NURBS knot vectors.",
                record.StartLineNumber));

            return;
        }

        log.Add(new DxfImportLogEntry(
            DxfImportLogSeverity.Warning,
            "Skipped SPLINE entity because it does not contain at least two readable control or fit points.",
            record.StartLineNumber));
    }

    private static IReadOnlyList<Point2D> ReadSplinePointSequence(
        DxfEntityRecord record,
        int xCode,
        int yCode,
        string pointKind,
        List<DxfImportLogEntry> log)
    {
        var points = new List<Point2D>();
        double? currentX = null;
        int currentXLineNumber = record.StartLineNumber;

        foreach (DxfCodePair pair in record.Pairs)
        {
            if (pair.Code == xCode)
            {
                if (currentX.HasValue)
                {
                    log.Add(new DxfImportLogEntry(
                        DxfImportLogSeverity.Warning,
                        $"Skipped SPLINE entity because a {pointKind} X coordinate is missing its matching Y coordinate.",
                        currentXLineNumber));

                    return Array.Empty<Point2D>();
                }

                if (!TryParseDouble(
                        pair,
                        record,
                        $"SPLINE {pointKind} X coordinate",
                        log,
                        out double x))
                {
                    return Array.Empty<Point2D>();
                }

                currentX = x;
                currentXLineNumber = pair.CodeLineNumber;
                continue;
            }

            if (pair.Code == yCode)
            {
                if (!currentX.HasValue)
                {
                    log.Add(new DxfImportLogEntry(
                        DxfImportLogSeverity.Warning,
                        $"Skipped SPLINE entity because a {pointKind} Y coordinate appears before its X coordinate.",
                        pair.CodeLineNumber));

                    return Array.Empty<Point2D>();
                }

                if (!TryParseDouble(
                        pair,
                        record,
                        $"SPLINE {pointKind} Y coordinate",
                        log,
                        out double y))
                {
                    return Array.Empty<Point2D>();
                }

                points.Add(new Point2D(currentX.Value, y));
                currentX = null;
            }
        }

        if (currentX.HasValue)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                $"Skipped SPLINE entity because a {pointKind} X coordinate is missing its matching Y coordinate.",
                currentXLineNumber));

            return Array.Empty<Point2D>();
        }

        return points;
    }

    private static bool IsSplineClosed(DxfEntityRecord record)
    {
        DxfCodePair? flagsPair = record.LastOrDefault(70);

        if (flagsPair is null)
        {
            return false;
        }

        if (!int.TryParse(
                flagsPair.Value.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int flags))
        {
            return false;
        }

        return (flags & 1) == 1;
    }

    private static void ImportMultilineText(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        if (!TryReadPoint(
                record,
                xCode: 10,
                yCode: 20,
                pointName: "insertion point",
                log,
                out Point2D insertionPoint) ||
            !TryReadRequiredString(
                record,
                code: 1,
                fieldName: "MTEXT value",
                log,
                out string text))
        {
            return;
        }

        text = FromDxfMTextContent(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped MTEXT entity because its text value is empty.",
                record.StartLineNumber));

            return;
        }

        double rotationDegrees = 0.0;

        if (!TryReadOptionalDouble(
                record,
                code: 50,
                fieldName: "MTEXT rotation",
                log,
                out double? parsedRotationDegrees))
        {
            return;
        }

        if (parsedRotationDegrees.HasValue)
        {
            rotationDegrees = parsedRotationDegrees.Value;
        }

        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        document.AddEntity(new MultilineTextEntity(
            insertionPoint,
            text,
            rotationDegrees,
            TextFormatId.Standard,
            layerId: layerId));
    }

    private static string FromDxfMTextContent(string text)
    {
        return text
            .Replace("\\P", "\n", StringComparison.Ordinal)
            .Replace("\\p", "\n", StringComparison.Ordinal)
            .Trim();
    }

    private static void ImportText(
        DxfEntityRecord record,
        CadDocument document,
        List<DxfImportLogEntry> log)
    {
        if (!TryReadPoint(
                record,
                xCode: 10,
                yCode: 20,
                pointName: "insertion point",
                log,
                out Point2D insertionPoint) ||
            !TryReadRequiredString(
                record,
                code: 1,
                fieldName: "TEXT value",
                log,
                out string text))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "Skipped TEXT entity because its text value is empty.",
                record.StartLineNumber));

            return;
        }

        double rotationDegrees = 0.0;

        if (!TryReadOptionalDouble(
                record,
                code: 50,
                fieldName: "TEXT rotation",
                log,
                out double? parsedRotationDegrees))
        {
            return;
        }

        if (parsedRotationDegrees.HasValue)
        {
            rotationDegrees = parsedRotationDegrees.Value;
        }

        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        document.AddEntity(new TextEntity(
            insertionPoint,
            text,
            rotationDegrees,
            TextFormatId.Standard,
            layerId: layerId));
    }


    private static bool TryReadPoint(
        DxfEntityRecord record,
        int xCode,
        int yCode,
        string pointName,
        List<DxfImportLogEntry> log,
        out Point2D point)
    {
        point = Point2D.Origin;

        if (!TryReadDouble(
                record,
                xCode,
                $"{record.Type} {pointName} X coordinate",
                log,
                out double x) ||
            !TryReadDouble(
                record,
                yCode,
                $"{record.Type} {pointName} Y coordinate",
                log,
                out double y))
        {
            return false;
        }

        point = new Point2D(x, y);
        return true;
    }

    private static bool TryReadDouble(
        DxfEntityRecord record,
        int code,
        string fieldName,
        List<DxfImportLogEntry> log,
        out double value)
    {
        value = 0;
        DxfCodePair? pair = record.LastOrDefault(code);

        if (pair is null)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                $"Skipped {record.Type} entity because required field '{fieldName}' with group code {code} is missing.",
                record.StartLineNumber));

            return false;
        }

        return TryParseDouble(
            pair.Value,
            record,
            fieldName,
            log,
            out value);
    }

    private static bool TryReadOptionalDouble(
        DxfEntityRecord record,
        int code,
        string fieldName,
        List<DxfImportLogEntry> log,
        out double? value)
    {
        value = null;
        DxfCodePair? pair = record.LastOrDefault(code);

        if (pair is null)
        {
            return true;
        }

        if (!TryParseDouble(
                pair.Value,
                record,
                fieldName,
                log,
                out double parsedValue))
        {
            return false;
        }

        value = parsedValue;
        return true;
    }

    private static bool TryReadRequiredString(
        DxfEntityRecord record,
        int code,
        string fieldName,
        List<DxfImportLogEntry> log,
        out string value)
    {
        value = string.Empty;
        DxfCodePair? pair = record.LastOrDefault(code);

        if (pair is null)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                $"Skipped {record.Type} entity because required field '{fieldName}' with group code {code} is missing.",
                record.StartLineNumber));

            return false;
        }

        value = pair.Value.Value;
        return true;
    }


    private static bool TryParseDouble(
        DxfCodePair pair,
        DxfEntityRecord record,
        string fieldName,
        List<DxfImportLogEntry> log,
        out double value)
    {
        if (!double.TryParse(
                pair.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                $"Skipped {record.Type} entity because field '{fieldName}' with group code {pair.Code} is not a valid number: '{pair.Value}'.",
                pair.ValueLineNumber));

            return false;
        }

        return true;
    }


    private static bool AreEquivalentAngles(
        double firstDegrees,
        double secondDegrees)
    {
        double normalizedFirst = NormalizeDegrees(firstDegrees);
        double normalizedSecond = NormalizeDegrees(secondDegrees);

        return Math.Abs(normalizedFirst - normalizedSecond) <= Tolerance.Default;
    }

    private static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360.0;

        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }


    private static int ReadOptionalInt(
        DxfRecord record,
        int code,
        int defaultValue)
    {
        DxfCodePair? pair = record.LastOrDefault(code);

        if (pair is null)
        {
            return defaultValue;
        }

        if (!int.TryParse(
                pair.Value.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value))
        {
            return defaultValue;
        }

        return value;
    }

    private static CadColor FromAci(int aciColor)
    {
        return aciColor switch
        {
            1 => CadColor.FromRgb(255, 0, 0),
            2 => CadColor.FromRgb(255, 255, 0),
            3 => CadColor.FromRgb(0, 255, 0),
            4 => CadColor.FromRgb(0, 255, 255),
            5 => CadColor.FromRgb(0, 0, 255),
            6 => CadColor.FromRgb(255, 0, 255),
            8 => CadColor.FromRgb(128, 128, 128),
            9 => CadColor.FromRgb(192, 192, 192),
            _ => CadColor.FromRgb(255, 255, 255),
        };
    }

    private static LineFormatId ToLineFormatId(string? dxfLineTypeName)
    {
        string normalizedName = string.IsNullOrWhiteSpace(dxfLineTypeName)
            ? "CONTINUOUS"
            : dxfLineTypeName.Trim().ToUpperInvariant();

        return normalizedName switch
        {
            "DASHED" => LineFormatId.Dashed,
            "DASHDOT" => LineFormatId.DashDot,
            "DASHDOTDOT" => LineFormatId.DashDotDot,
            "CENTER" => LineFormatId.Axis,
            "CENTER2" => LineFormatId.Axis,
            "CENTERX2" => LineFormatId.Axis,
            _ => LineFormatId.Continuous,
        };
    }

    private static LineWeight FromDxfLineWeight(int dxfLineWeight)
    {
        if (dxfLineWeight < 0)
        {
            return LineWeight.FromMillimeters(0.25);
        }

        return LineWeight.FromMillimeters(dxfLineWeight / 100.0);
    }

    private static string GetLayerName(DxfEntityRecord record)
    {
        DxfCodePair? layerPair = record.LastOrDefault(8);

        if (layerPair is null || string.IsNullOrWhiteSpace(layerPair.Value.Value))
        {
            return LayerId.Default.Value;
        }

        return layerPair.Value.Value.Trim();
    }

    private static LayerId EnsureLayer(
        CadDocument document,
        string layerName)
    {
        string normalizedLayerName = string.IsNullOrWhiteSpace(layerName)
            ? LayerId.Default.Value
            : layerName.Trim();

        LayerId layerId = new(normalizedLayerName);

        if (document.Layers.Contains(layerId))
        {
            return layerId;
        }

        document.Layers.Add(new Layer(
            layerId,
            normalizedLayerName));

        return layerId;
    }

    private readonly record struct DxfPolylineVertex(
        Point2D Point,
        double Bulge);

    private sealed class DxfTable
    {
        public DxfTable(
            string name,
            IReadOnlyList<DxfCodePair> pairs,
            int startLineNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "DXF table name cannot be empty.",
                    nameof(name));
            }

            Name = name.Trim().ToUpperInvariant();
            Pairs = pairs ?? throw new ArgumentNullException(nameof(pairs));
            StartLineNumber = startLineNumber;
        }

        public string Name { get; }

        public IReadOnlyList<DxfCodePair> Pairs { get; }

        public int StartLineNumber { get; }
    }

    private sealed class DxfTableRecord : DxfRecord
    {
        public DxfTableRecord(
            string type,
            IReadOnlyList<DxfCodePair> pairs,
            int startLineNumber)
            : base(
                type,
                pairs,
                startLineNumber)
        {
        }
    }

    private sealed class DxfEntityRecord : DxfRecord
    {
        public DxfEntityRecord(
            string type,
            IReadOnlyList<DxfCodePair> pairs,
            int startLineNumber)
            : base(
                type,
                pairs,
                startLineNumber)
        {
        }
    }

    private abstract class DxfRecord
    {
        protected DxfRecord(
            string type,
            IReadOnlyList<DxfCodePair> pairs,
            int startLineNumber)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException(
                    "DXF record type cannot be empty.",
                    nameof(type));
            }

            Type = type.Trim().ToUpperInvariant();
            Pairs = pairs ?? throw new ArgumentNullException(nameof(pairs));
            StartLineNumber = startLineNumber;
        }

        public string Type { get; }

        public IReadOnlyList<DxfCodePair> Pairs { get; }

        public int StartLineNumber { get; }

        public DxfCodePair? LastOrDefault(int code)
        {
            for (int index = Pairs.Count - 1; index >= 0; index--)
            {
                DxfCodePair pair = Pairs[index];

                if (pair.Code == code)
                {
                    return pair;
                }
            }

            return null;
        }
    }
}
