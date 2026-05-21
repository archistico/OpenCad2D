using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces the document dimension style collection and current style as a single undoable operation.
/// </summary>
public sealed class UpdateDimensionStylesCommand : ICadCommand
{
    private readonly IReadOnlyList<DimensionStyle> _oldStyles;
    private readonly IReadOnlyList<DimensionStyle> _newStyles;
    private readonly DimensionStyleId _oldCurrentStyleId;
    private readonly DimensionStyleId _newCurrentStyleId;

    public UpdateDimensionStylesCommand(
        IEnumerable<DimensionStyle> oldStyles,
        IEnumerable<DimensionStyle> newStyles,
        DimensionStyleId oldCurrentStyleId,
        DimensionStyleId newCurrentStyleId)
    {
        ArgumentNullException.ThrowIfNull(oldStyles);
        ArgumentNullException.ThrowIfNull(newStyles);

        _oldStyles = oldStyles.ToList();
        _newStyles = newStyles.ToList();
        _oldCurrentStyleId = oldCurrentStyleId;
        _newCurrentStyleId = newCurrentStyleId;
    }

    public string Name => "Update Dimension Styles";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var nextCollection = new DimensionStyleCollection(_newStyles);
        EnsureStandardExists(nextCollection);

        if (!nextCollection.Contains(_newCurrentStyleId))
        {
            throw new InvalidOperationException(
                $"Current dimension style '{_newCurrentStyleId}' does not exist in the new collection.");
        }

        document.ReplaceDimensionStyles(nextCollection);
        document.SetCurrentDimensionStyle(_newCurrentStyleId);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var previousCollection = new DimensionStyleCollection(_oldStyles);
        EnsureStandardExists(previousCollection);

        document.ReplaceDimensionStyles(previousCollection);
        document.SetCurrentDimensionStyle(_oldCurrentStyleId);
    }

    private static void EnsureStandardExists(DimensionStyleCollection styles)
    {
        if (!styles.Contains(DimensionStyleId.Standard))
        {
            throw new InvalidOperationException(
                "Dimension style collection must contain the built-in Standard style.");
        }
    }
}
