using System.Globalization;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
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

        IReadOnlyList<DxfCodePair> pairs = _reader.Read(content);

        return ImportPairs(pairs);
    }

    /// <inheritdoc />
    public DxfImportResult ImportFile(string filePath)
    {
        IReadOnlyList<DxfCodePair> pairs = _reader.ReadFile(filePath);

        return ImportPairs(pairs);
    }

    private DxfImportResult ImportPairs(IReadOnlyList<DxfCodePair> pairs)
    {
        var document = new CadDocument();
        var log = new List<DxfImportLogEntry>();
        IReadOnlyList<DxfSection> sections = _sectionReader.ReadSections(pairs);

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

        bool hasBulge = vertices.Any(vertex =>
            !Tolerance.AreEqual(vertex.Bulge, 0));

        if (hasBulge)
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                "LWPOLYLINE bulge values are not supported yet; curved segments were imported as straight segments.",
                record.StartLineNumber));
        }

        bool isClosed = IsLightweightPolylineClosed(record);
        LayerId layerId = EnsureLayer(
            document,
            GetLayerName(record));

        document.AddEntity(new PolylineEntity(
            vertices.Select(vertex => vertex.Point),
            isClosed,
            layerId: layerId));
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

    private sealed class DxfEntityRecord
    {
        public DxfEntityRecord(
            string type,
            IReadOnlyList<DxfCodePair> pairs,
            int startLineNumber)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException(
                    "DXF entity type cannot be empty.",
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
