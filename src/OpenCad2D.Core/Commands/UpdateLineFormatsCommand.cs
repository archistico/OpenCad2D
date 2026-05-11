using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces the document line format collection as a single undoable operation.
/// Layers that reference removed formats are automatically rebased to Continuous.
/// </summary>
public sealed class UpdateLineFormatsCommand : ICadCommand
{
    private readonly IReadOnlyList<LineFormat> _oldFormats;
    private readonly IReadOnlyList<LineFormat> _newFormats;
    private IReadOnlyList<Layer>? _oldLayers;

    public UpdateLineFormatsCommand(
        IEnumerable<LineFormat> oldFormats,
        IEnumerable<LineFormat> newFormats)
    {
        ArgumentNullException.ThrowIfNull(oldFormats);
        ArgumentNullException.ThrowIfNull(newFormats);

        _oldFormats = oldFormats.ToList();
        _newFormats = newFormats.ToList();
    }

    public UpdateLineFormatsCommand(
        LineFormatCollection oldFormats,
        LineFormatCollection newFormats)
        : this(oldFormats.All, newFormats.All)
    {
    }

    public string Name => "Update Line Formats";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var nextCollection = new LineFormatCollection(_newFormats);

        EnsureContinuousExists(nextCollection);

        _oldLayers ??= document.Layers.All.ToList();

        document.ReplaceLineFormats(nextCollection);
        RebaseLayersWithMissingLineFormats(
            document,
            nextCollection);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var previousCollection = new LineFormatCollection(_oldFormats);

        EnsureContinuousExists(previousCollection);

        document.ReplaceLineFormats(previousCollection);

        if (_oldLayers is not null)
        {
            document.Layers.ReplaceAll(_oldLayers);
        }
    }

    private static void EnsureContinuousExists(LineFormatCollection formats)
    {
        if (!formats.Contains(LineFormatId.Continuous))
        {
            throw new InvalidOperationException(
                "Line format collection must contain the built-in Continuous format.");
        }
    }

    private static void RebaseLayersWithMissingLineFormats(
        CadDocument document,
        LineFormatCollection formats)
    {
        List<Layer> rebasedLayers = document.Layers.All
            .Select(layer => formats.Contains(layer.LineFormatId)
                ? layer
                : layer.WithLineFormat(LineFormatId.Continuous))
            .ToList();

        document.Layers.ReplaceAll(rebasedLayers);
    }
}
