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
                $"LINE {pointName} X coordinate",
                log,
                out double x) ||
            !TryReadDouble(
                record,
                yCode,
                $"LINE {pointName} Y coordinate",
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

        if (!double.TryParse(
                pair.Value.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
        {
            log.Add(new DxfImportLogEntry(
                DxfImportLogSeverity.Warning,
                $"Skipped {record.Type} entity because field '{fieldName}' with group code {code} is not a valid number: '{pair.Value.Value}'.",
                pair.Value.ValueLineNumber));

            return false;
        }

        return true;
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
