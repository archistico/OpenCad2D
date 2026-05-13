using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;

namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Aggregated counters produced from the imported document and diagnostic log.
/// </summary>
public sealed class DxfImportStatistics
{
    private DxfImportStatistics(
        IReadOnlyDictionary<EntityKind, int> importedEntityCounts,
        int importedLayerCount,
        int warningCount,
        int errorCount,
        int skippedEntityWarningCount)
    {
        ImportedEntityCounts = importedEntityCounts ?? throw new ArgumentNullException(nameof(importedEntityCounts));
        ImportedLayerCount = importedLayerCount;
        WarningCount = warningCount;
        ErrorCount = errorCount;
        SkippedEntityWarningCount = skippedEntityWarningCount;
    }

    /// <summary>
    /// Gets the number of imported entities grouped by OpenCad2D entity kind.
    /// </summary>
    public IReadOnlyDictionary<EntityKind, int> ImportedEntityCounts { get; }

    /// <summary>
    /// Gets the total number of imported entities.
    /// </summary>
    public int TotalImportedEntities => ImportedEntityCounts.Values.Sum();

    /// <summary>
    /// Gets the number of layers present in the imported document, including layer 0.
    /// </summary>
    public int ImportedLayerCount { get; }

    /// <summary>
    /// Gets the number of warning diagnostics produced during import.
    /// </summary>
    public int WarningCount { get; }

    /// <summary>
    /// Gets the number of error diagnostics produced during import.
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    /// Gets how many warning diagnostics describe skipped DXF records.
    /// </summary>
    public int SkippedEntityWarningCount { get; }

    /// <summary>
    /// Builds aggregate import counters from the resulting document and log.
    /// </summary>
    public static DxfImportStatistics From(
        CadDocument document,
        IReadOnlyList<DxfImportLogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(log);

        Dictionary<EntityKind, int> importedEntityCounts = document.Entities.All
            .GroupBy(entity => entity.Kind)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        int warningCount = log.Count(entry => entry.Severity == DxfImportLogSeverity.Warning);
        int errorCount = log.Count(entry => entry.Severity == DxfImportLogSeverity.Error);
        int skippedEntityWarningCount = log.Count(entry =>
            entry.Severity == DxfImportLogSeverity.Warning &&
            entry.Message.StartsWith(
                "Skipped ",
                StringComparison.OrdinalIgnoreCase));

        return new DxfImportStatistics(
            importedEntityCounts,
            document.Layers.Count,
            warningCount,
            errorCount,
            skippedEntityWarningCount);
    }

    /// <summary>
    /// Returns the imported entity count for the requested kind, or zero when none were imported.
    /// </summary>
    public int GetImportedEntityCount(EntityKind kind)
    {
        return ImportedEntityCounts.TryGetValue(kind, out int count)
            ? count
            : 0;
    }
}
