using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Dxf.Import;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels.DxfImport;

/// <summary>
/// Read-only view-model used by the DXF import report dialog.
/// </summary>
public sealed class DxfImportReportWindowViewModel
{
    public DxfImportReportWindowViewModel(DxfImportResult result)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
        EntityRows = CreateEntityRows(result);
        DiagnosticRows = CreateDiagnosticRows(result);
    }

    public DxfImportResult Result { get; }

    public string StatusTitle => Result.HasErrors
        ? "DXF import failed"
        : Result.HasWarnings
            ? "DXF imported with warnings"
            : "DXF imported successfully";

    public string StatusDescription => Result.HasErrors
        ? "The drawing was not replaced because the DXF import produced one or more errors."
        : Result.HasWarnings
            ? "The drawing was imported, but some DXF records were skipped or simplified."
            : "The drawing was imported without diagnostics.";

    public int TotalImportedEntities => Result.Statistics.TotalImportedEntities;

    public int ImportedLayerCount => Result.Statistics.ImportedLayerCount;

    public int WarningCount => Result.Statistics.WarningCount;

    public int ErrorCount => Result.Statistics.ErrorCount;

    public int SkippedRecordCount => Result.Statistics.SkippedEntityWarningCount;

    public bool HasEntityRows => EntityRows.Count > 0;

    public bool HasDiagnosticRows => DiagnosticRows.Count > 0;

    public IReadOnlyList<DxfImportEntityCountRowViewModel> EntityRows { get; }

    public IReadOnlyList<DxfImportDiagnosticRowViewModel> DiagnosticRows { get; }

    private static IReadOnlyList<DxfImportEntityCountRowViewModel> CreateEntityRows(DxfImportResult result)
    {
        EntityKind[] order =
        {
            EntityKind.Line,
            EntityKind.Circle,
            EntityKind.Arc,
            EntityKind.Point,
            EntityKind.Polyline,
            EntityKind.Text,
            EntityKind.HorizontalDimension,
            EntityKind.VerticalDimension,
            EntityKind.AlignedDimension,
            EntityKind.RadiusDimension,
            EntityKind.DiameterDimension,
            EntityKind.AngularDimension
        };

        return result.Statistics.ImportedEntityCounts
            .OrderBy(pair => Array.IndexOf(order, pair.Key) < 0 ? int.MaxValue : Array.IndexOf(order, pair.Key))
            .ThenBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
            .Select(pair => new DxfImportEntityCountRowViewModel(pair.Key.ToString(), pair.Value))
            .ToList();
    }

    private static IReadOnlyList<DxfImportDiagnosticRowViewModel> CreateDiagnosticRows(DxfImportResult result)
    {
        return result.Log
            .Select(entry => new DxfImportDiagnosticRowViewModel(
                entry.Severity.ToString(),
                entry.LineNumber?.ToString() ?? "-",
                entry.Message))
            .ToList();
    }
}

/// <summary>
/// One imported entity count row shown in the DXF import report dialog.
/// </summary>
public sealed class DxfImportEntityCountRowViewModel
{
    public DxfImportEntityCountRowViewModel(string entityKind, int count)
    {
        if (string.IsNullOrWhiteSpace(entityKind))
        {
            throw new ArgumentException(
                "Entity kind cannot be empty.",
                nameof(entityKind));
        }

        EntityKind = entityKind;
        Count = count;
    }

    public string EntityKind { get; }

    public int Count { get; }
}

/// <summary>
/// One diagnostic row shown in the DXF import report dialog.
/// </summary>
public sealed class DxfImportDiagnosticRowViewModel
{
    public DxfImportDiagnosticRowViewModel(
        string severity,
        string line,
        string message)
    {
        Severity = string.IsNullOrWhiteSpace(severity)
            ? throw new ArgumentException("Severity cannot be empty.", nameof(severity))
            : severity;

        Line = string.IsNullOrWhiteSpace(line)
            ? "-"
            : line;

        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("Message cannot be empty.", nameof(message))
            : message;
    }

    public string Severity { get; }

    public string Line { get; }

    public string Message { get; }
}
