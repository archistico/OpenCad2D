using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Core.Commands;

/// <summary>
/// Replaces the document text format collection as a single undoable operation.
/// Text entities that reference removed formats are automatically rebased to Standard.
/// </summary>
public sealed class UpdateTextFormatsCommand : ICadCommand
{
    private readonly IReadOnlyList<TextFormat> _oldFormats;
    private readonly IReadOnlyList<TextFormat> _newFormats;
    private IReadOnlyList<CadEntity>? _oldTextEntities;

    public UpdateTextFormatsCommand(
        IEnumerable<TextFormat> oldFormats,
        IEnumerable<TextFormat> newFormats)
    {
        ArgumentNullException.ThrowIfNull(oldFormats);
        ArgumentNullException.ThrowIfNull(newFormats);

        _oldFormats = oldFormats.ToList();
        _newFormats = newFormats.ToList();
    }

    public UpdateTextFormatsCommand(
        TextFormatCollection oldFormats,
        TextFormatCollection newFormats)
        : this(oldFormats.All, newFormats.All)
    {
    }

    public string Name => "Update Text Formats";

    public void Execute(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var nextCollection = new TextFormatCollection(_newFormats);

        EnsureStandardExists(nextCollection);

        _oldTextEntities ??= document.Entities.All
            .Where(entity => entity is TextEntity or MultilineTextEntity)
            .ToList();

        document.ReplaceTextFormats(nextCollection);
        RebaseTextEntitiesWithMissingTextFormats(
            document,
            nextCollection);
    }

    public void Undo(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var previousCollection = new TextFormatCollection(_oldFormats);

        EnsureStandardExists(previousCollection);

        document.ReplaceTextFormats(previousCollection);

        if (_oldTextEntities is null)
        {
            return;
        }

        foreach (CadEntity oldTextEntity in _oldTextEntities)
        {
            if (document.Entities.Contains(oldTextEntity.Id))
            {
                document.Entities.Replace(oldTextEntity);
            }
        }
    }

    private static void EnsureStandardExists(TextFormatCollection formats)
    {
        if (!formats.Contains(TextFormatId.Standard))
        {
            throw new InvalidOperationException(
                "Text format collection must contain the built-in Standard format.");
        }
    }

    private static void RebaseTextEntitiesWithMissingTextFormats(
        CadDocument document,
        TextFormatCollection formats)
    {
        List<CadEntity> rebasedTextEntities = document.Entities.All
            .Where(entity =>
                entity is TextEntity text && !formats.Contains(text.TextFormatId) ||
                entity is MultilineTextEntity multilineText && !formats.Contains(multilineText.TextFormatId))
            .Select(entity => entity switch
            {
                TextEntity text => text.WithTextFormat(TextFormatId.Standard),
                MultilineTextEntity multilineText => multilineText.WithTextFormat(TextFormatId.Standard),
                _ => entity
            })
            .ToList();

        foreach (CadEntity rebasedTextEntity in rebasedTextEntities)
        {
            document.Entities.Replace(rebasedTextEntity);
        }
    }
}
