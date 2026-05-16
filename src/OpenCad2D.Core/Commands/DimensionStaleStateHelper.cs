using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Captures and updates stale markers for non-associative dimensions.
/// </summary>
internal static class DimensionStaleStateHelper
{
    public static List<DimensionEntity> Capture(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.Entities.All
            .OfType<DimensionEntity>()
            .ToList();
    }

    public static void MarkAllStale(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        List<DimensionEntity> replacements = document.Entities.All
            .OfType<DimensionEntity>()
            .Where(dimension => !dimension.IsStale)
            .Select(dimension => dimension.WithStaleState(true))
            .ToList();

        if (replacements.Count == 0)
        {
            return;
        }

        foreach (DimensionEntity replacement in replacements)
        {
            document.Entities.Replace(replacement);
        }
    }

    public static void Restore(CadDocument document, IReadOnlyList<DimensionEntity>? capturedDimensions)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (capturedDimensions is null || capturedDimensions.Count == 0)
        {
            return;
        }

        List<DimensionEntity> replacements = capturedDimensions
            .Where(dimension => document.Entities.Contains(dimension.Id))
            .ToList();

        if (replacements.Count == 0)
        {
            return;
        }

        foreach (DimensionEntity replacement in replacements)
        {
            document.Entities.Replace(replacement);
        }
    }
}
