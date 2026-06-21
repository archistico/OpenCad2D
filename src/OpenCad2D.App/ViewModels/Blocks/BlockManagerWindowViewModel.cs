using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Blocks;

public sealed class BlockManagerWindowViewModel : INotifyPropertyChanged
{
    private readonly CadDocument _document;
    private readonly Dictionary<BlockDefinitionId, string> _originalNames = new();
    private EditableBlockDefinitionViewModel? _selectedBlock;
    private string _validationMessage = string.Empty;

    public BlockManagerWindowViewModel(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        Blocks = new ObservableCollection<EditableBlockDefinitionViewModel>();
        RebuildBlocks(
            document.BlockDefinitions.All,
            selectedBlockDefinitionId: null);
    }

    public ObservableCollection<EditableBlockDefinitionViewModel> Blocks { get; }

    public EditableBlockDefinitionViewModel? SelectedBlock
    {
        get => _selectedBlock;
        set
        {
            if (ReferenceEquals(_selectedBlock, value))
            {
                return;
            }

            _selectedBlock = value;
            OnPropertyChanged();
            OnSelectedBlockStateChanged();
        }
    }

    public bool HasSelectedBlock => SelectedBlock is not null;

    public bool CanDeleteSelectedBlock => SelectedBlock?.CanDelete == true;

    public bool CanDuplicateSelectedBlock => SelectedBlock is not null &&
                                             SelectedBlock.HasBlockingDiagnostic == false;

    public bool CanInsertSelectedBlock => SelectedBlock is not null &&
                                          SelectedBlock.HasRecursiveReference == false;

    public int MissingDrawingReferenceCount { get; private set; }

    public int MissingNestedReferenceCount { get; private set; }

    public int PurgeCandidateCount { get; private set; }

    public bool CanPurgeUnusedBlocks => PurgeCandidateCount > 0;

    public int PendingRenameCount => Blocks.Count(block => block.IsRenamed);

    public bool HasPendingRenames => PendingRenameCount > 0;

    public bool CanResetBlockNames => HasPendingRenames;

    public string RenameSummaryText
    {
        get
        {
            int pendingRenameCount = PendingRenameCount;

            return pendingRenameCount == 0
                ? "No pending block renames."
                : pendingRenameCount == 1
                    ? "1 pending block rename. It will be applied when you press OK."
                    : $"{pendingRenameCount} pending block renames. They will be applied when you press OK.";
        }
    }

    public bool HasDocumentDiagnostics => MissingDrawingReferenceCount > 0 ||
                                          MissingNestedReferenceCount > 0;

    public string DocumentDiagnosticsText
    {
        get
        {
            if (!HasDocumentDiagnostics)
            {
                return "No missing block references detected.";
            }

            var parts = new List<string>();

            if (MissingDrawingReferenceCount > 0)
            {
                parts.Add(MissingDrawingReferenceCount == 1
                    ? "1 drawing block reference points to a missing definition"
                    : $"{MissingDrawingReferenceCount} drawing block references point to missing definitions");
            }

            if (MissingNestedReferenceCount > 0)
            {
                parts.Add(MissingNestedReferenceCount == 1
                    ? "1 nested block reference points to a missing definition"
                    : $"{MissingNestedReferenceCount} nested block references point to missing definitions");
            }

            return string.Join("; ", parts) + ".";
        }
    }

    public string SummaryText
    {
        get
        {
            int usedCount = Blocks.Count(block => block.IsReachableFromDrawing);
            int issueCount = Blocks.Count(block => block.HasDiagnosticIssue) +
                             (HasDocumentDiagnostics ? 1 : 0);

            string blockText = Blocks.Count == 1
                ? "1 block definition"
                : $"{Blocks.Count} block definitions";

            string purgeText = PurgeCandidateCount == 1
                ? "1 purge candidate"
                : $"{PurgeCandidateCount} purge candidates";

            int pendingRenameCount = PendingRenameCount;
            string renameText = pendingRenameCount == 1
                ? "1 pending rename"
                : $"{pendingRenameCount} pending renames";

            string summary = issueCount == 0
                ? $"{blockText} · {usedCount} drawing-used · {purgeText}"
                : $"{blockText} · {usedCount} drawing-used · {purgeText} · {issueCount} diagnostics";

            return pendingRenameCount == 0
                ? summary
                : $"{summary} · {renameText}";
        }
    }

    public string SelectedBlockDetailsText
    {
        get
        {
            if (SelectedBlock is null)
            {
                return "No block selected.";
            }

            return SelectedBlock.DetailsText;
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage == value)
            {
                return;
            }

            _validationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidationMessage));
        }
    }

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    public void DuplicateSelectedBlock()
    {
        if (SelectedBlock is null)
        {
            return;
        }

        if (SelectedBlock.HasBlockingDiagnostic)
        {
            ValidationMessage = $"Block '{SelectedBlock.Name.Trim()}' cannot be duplicated because it has blocking diagnostics.";
            return;
        }

        if (!TryGetValidatedBlockDefinitions(out List<BlockDefinition> definitions))
        {
            return;
        }

        BlockDefinition? sourceDefinition = definitions.FirstOrDefault(definition => definition.Id == SelectedBlock.Id);

        if (sourceDefinition is null)
        {
            ValidationMessage = "The selected block no longer exists.";
            return;
        }

        string duplicateName = CreateUniqueCopyName(
            sourceDefinition.Name,
            definitions.Select(definition => definition.Name));

        var duplicate = new BlockDefinition(
            BlockDefinitionId.New(),
            duplicateName,
            sourceDefinition.Entities.Select(entity => entity.WithId(EntityId.New())).ToList());

        definitions.Add(duplicate);
        RebuildBlocks(
            definitions,
            duplicate.Id);
        ClearValidation();
    }

    public void DeleteSelectedBlock()
    {
        if (SelectedBlock is null)
        {
            return;
        }

        if (!SelectedBlock.CanDelete)
        {
            ValidationMessage = SelectedBlock.TotalReferenceCount > 0
                ? "The selected block cannot be deleted because it is still referenced by the drawing or by another block definition. Use Purge Unused to remove drawing-unreachable block trees as one safe operation."
                : "The selected block cannot be deleted because it has blocking diagnostics.";
            return;
        }

        BlockDefinitionId deletedId = SelectedBlock.Id;
        List<BlockDefinition> retainedDefinitions = Blocks
            .Where(block => block.Id != deletedId)
            .Select(block => block.ToBlockDefinition())
            .ToList();

        RebuildBlocks(
            retainedDefinitions,
            retainedDefinitions.FirstOrDefault()?.Id);
        ClearValidation();
    }

    public void PurgeUnusedBlocks()
    {
        if (!TryGetValidatedBlockDefinitions(out List<BlockDefinition> definitions, allowBlockingDiagnostics: true))
        {
            return;
        }

        HashSet<BlockDefinitionId> reachableIds = FindDrawingReachableDefinitionIds(
            definitions,
            _document.Entities.All.OfType<BlockReferenceEntity>());

        List<BlockDefinition> retainedDefinitions = definitions
            .Where(definition => reachableIds.Contains(definition.Id))
            .ToList();

        int purgedCount = definitions.Count - retainedDefinitions.Count;

        if (purgedCount == 0)
        {
            ValidationMessage = "No unused block definitions can be purged.";
            return;
        }

        BlockDefinitionId? nextSelectedId = SelectedBlock is not null &&
                                            retainedDefinitions.Any(definition => definition.Id == SelectedBlock.Id)
            ? SelectedBlock.Id
            : retainedDefinitions.FirstOrDefault()?.Id;

        RebuildBlocks(
            retainedDefinitions,
            nextSelectedId);
        ClearValidation();
    }

    public void ResetBlockNames()
    {
        if (!HasPendingRenames)
        {
            return;
        }

        foreach (EditableBlockDefinitionViewModel block in Blocks)
        {
            block.ResetName();
        }

        ClearValidation();
        RefreshRenameState();
    }

    public bool TryBuildResult(
        BlockManagerAction action,
        out BlockManagerResult result)
    {
        result = new BlockManagerResult(
            action,
            Array.Empty<BlockDefinition>(),
            null,
            null);

        if (!TryGetValidatedBlockDefinitions(out List<BlockDefinition> definitions))
        {
            return false;
        }

        if (action == BlockManagerAction.InsertSelected && SelectedBlock is null)
        {
            ValidationMessage = "Select a block definition before inserting it.";
            return false;
        }

        if (action == BlockManagerAction.InsertSelected &&
            SelectedBlock?.HasRecursiveReference == true)
        {
            ValidationMessage = "The selected block cannot be inserted because it contains a recursive block reference.";
            return false;
        }

        result = new BlockManagerResult(
            action,
            definitions,
            SelectedBlock?.Id,
            SelectedBlock?.Name.Trim());

        ClearValidation();
        return true;
    }

    private void RebuildBlocks(
        IEnumerable<BlockDefinition> blockDefinitions,
        BlockDefinitionId? selectedBlockDefinitionId)
    {
        List<BlockDefinition> definitions = blockDefinitions.ToList();
        HashSet<BlockDefinitionId> definitionIds = definitions
            .Select(definition => definition.Id)
            .ToHashSet();

        IReadOnlyDictionary<BlockDefinitionId, int> drawingReferenceCounts = _document.Entities.All
            .OfType<BlockReferenceEntity>()
            .GroupBy(reference => reference.BlockDefinitionId)
            .ToDictionary(group => group.Key, group => group.Count());

        IReadOnlyDictionary<BlockDefinitionId, int> nestedReferenceCounts = definitions
            .SelectMany(definition => definition.Entities.OfType<BlockReferenceEntity>())
            .GroupBy(reference => reference.BlockDefinitionId)
            .ToDictionary(group => group.Key, group => group.Count());

        HashSet<BlockDefinitionId> recursiveDefinitionIds = FindRecursiveDefinitionIds(definitions);
        HashSet<BlockDefinitionId> drawingReachableIds = FindDrawingReachableDefinitionIds(
            definitions,
            _document.Entities.All.OfType<BlockReferenceEntity>());

        MissingDrawingReferenceCount = _document.Entities.All
            .OfType<BlockReferenceEntity>()
            .Count(reference => !definitionIds.Contains(reference.BlockDefinitionId));

        MissingNestedReferenceCount = definitions
            .SelectMany(definition => definition.Entities.OfType<BlockReferenceEntity>())
            .Count(reference => !definitionIds.Contains(reference.BlockDefinitionId));

        PurgeCandidateCount = definitions.Count(definition => !drawingReachableIds.Contains(definition.Id));

        foreach (BlockDefinition definition in definitions)
        {
            _originalNames.TryAdd(
                definition.Id,
                definition.Name);
        }

        HashSet<BlockDefinitionId> retainedDefinitionIds = definitionIds;
        foreach (BlockDefinitionId originalId in _originalNames.Keys.ToList())
        {
            if (!retainedDefinitionIds.Contains(originalId))
            {
                _originalNames.Remove(originalId);
            }
        }

        List<EditableBlockDefinitionViewModel> nextBlocks = definitions
            .OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .Select(definition => new EditableBlockDefinitionViewModel(
                definition,
                GetCount(drawingReferenceCounts, definition.Id),
                GetCount(nestedReferenceCounts, definition.Id),
                definition.Entities
                    .OfType<BlockReferenceEntity>()
                    .Count(reference => !definitionIds.Contains(reference.BlockDefinitionId)),
                definition.Entities.OfType<BlockReferenceEntity>().Any(),
                definition.Entities
                    .OfType<BlockReferenceEntity>()
                    .Any(reference => reference.BlockDefinitionId == definition.Id),
                recursiveDefinitionIds.Contains(definition.Id),
                drawingReachableIds.Contains(definition.Id),
                _originalNames[definition.Id]))
            .ToList();

        foreach (EditableBlockDefinitionViewModel block in Blocks)
        {
            block.PropertyChanged -= Block_PropertyChanged;
        }

        Blocks.Clear();

        foreach (EditableBlockDefinitionViewModel block in nextBlocks)
        {
            block.PropertyChanged += Block_PropertyChanged;
            Blocks.Add(block);
        }

        SelectedBlock = selectedBlockDefinitionId is null
            ? Blocks.FirstOrDefault()
            : Blocks.FirstOrDefault(block => block.Id == selectedBlockDefinitionId.Value) ??
              Blocks.FirstOrDefault();

        RefreshDerivedState();
    }

    private void RefreshDerivedState()
    {
        OnPropertyChanged(nameof(MissingDrawingReferenceCount));
        OnPropertyChanged(nameof(MissingNestedReferenceCount));
        OnPropertyChanged(nameof(PurgeCandidateCount));
        OnPropertyChanged(nameof(CanPurgeUnusedBlocks));
        RefreshRenameState();
        OnPropertyChanged(nameof(HasDocumentDiagnostics));
        OnPropertyChanged(nameof(DocumentDiagnosticsText));
        OnPropertyChanged(nameof(SummaryText));
        OnSelectedBlockStateChanged();
    }

    private void RefreshRenameState()
    {
        OnPropertyChanged(nameof(PendingRenameCount));
        OnPropertyChanged(nameof(HasPendingRenames));
        OnPropertyChanged(nameof(CanResetBlockNames));
        OnPropertyChanged(nameof(RenameSummaryText));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(SelectedBlockDetailsText));
    }

    private void Block_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditableBlockDefinitionViewModel.Name) or
            nameof(EditableBlockDefinitionViewModel.IsRenamed) or
            nameof(EditableBlockDefinitionViewModel.RenameStatusText))
        {
            ClearValidation();
            RefreshRenameState();
        }
    }

    private void OnSelectedBlockStateChanged()
    {
        OnPropertyChanged(nameof(HasSelectedBlock));
        OnPropertyChanged(nameof(CanDeleteSelectedBlock));
        OnPropertyChanged(nameof(CanDuplicateSelectedBlock));
        OnPropertyChanged(nameof(CanInsertSelectedBlock));
        OnPropertyChanged(nameof(SelectedBlockDetailsText));
    }

    private bool TryGetValidatedBlockDefinitions(
        out List<BlockDefinition> definitions,
        bool allowBlockingDiagnostics = false)
    {
        definitions = new List<BlockDefinition>();

        foreach (EditableBlockDefinitionViewModel block in Blocks)
        {
            string? validation = block.Validate(
                allowBlockingDiagnostics);

            if (validation is not null)
            {
                ValidationMessage = validation;
                SelectedBlock = block;
                return false;
            }

            definitions.Add(block.ToBlockDefinition());
        }

        var duplicateName = Blocks
            .GroupBy(block => block.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
        {
            ValidationMessage = $"Duplicate block name '{duplicateName.Key}'.";
            SelectedBlock = duplicateName.First();
            return false;
        }

        return true;
    }

    private void ClearValidation()
    {
        ValidationMessage = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private static int GetCount(
        IReadOnlyDictionary<BlockDefinitionId, int> counts,
        BlockDefinitionId id)
    {
        return counts.TryGetValue(id, out int count)
            ? count
            : 0;
    }

    private static string CreateUniqueCopyName(
        string sourceName,
        IEnumerable<string> existingNames)
    {
        string trimmedSourceName = string.IsNullOrWhiteSpace(sourceName)
            ? "Block"
            : sourceName.Trim();

        var usedNames = new HashSet<string>(
            existingNames.Select(name => name.Trim()),
            StringComparer.OrdinalIgnoreCase);

        string firstCandidate = trimmedSourceName + " Copy";

        if (!usedNames.Contains(firstCandidate))
        {
            return firstCandidate;
        }

        for (int index = 2; index < 10000; index++)
        {
            string candidate = $"{trimmedSourceName} Copy {index}";

            if (!usedNames.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{trimmedSourceName} Copy {Guid.NewGuid():N}";
    }

    private static HashSet<BlockDefinitionId> FindDrawingReachableDefinitionIds(
        IReadOnlyList<BlockDefinition> definitions,
        IEnumerable<BlockReferenceEntity> drawingReferences)
    {
        var definitionsById = definitions.ToDictionary(definition => definition.Id);
        var reachableIds = new HashSet<BlockDefinitionId>();
        var pending = new Stack<BlockDefinitionId>(
            drawingReferences.Select(reference => reference.BlockDefinitionId));

        while (pending.Count > 0)
        {
            BlockDefinitionId currentId = pending.Pop();

            if (!reachableIds.Add(currentId))
            {
                continue;
            }

            if (!definitionsById.TryGetValue(currentId, out BlockDefinition? definition))
            {
                continue;
            }

            foreach (BlockReferenceEntity nestedReference in definition.Entities.OfType<BlockReferenceEntity>())
            {
                pending.Push(nestedReference.BlockDefinitionId);
            }
        }

        reachableIds.IntersectWith(definitionsById.Keys);
        return reachableIds;
    }

    private static HashSet<BlockDefinitionId> FindRecursiveDefinitionIds(
        IReadOnlyList<BlockDefinition> definitions)
    {
        var byId = definitions.ToDictionary(definition => definition.Id);
        var recursiveIds = new HashSet<BlockDefinitionId>();

        foreach (BlockDefinition definition in definitions)
        {
            var visiting = new HashSet<BlockDefinitionId>();
            var visited = new HashSet<BlockDefinitionId>();

            if (ReferencesDefinition(
                    definition.Id,
                    definition.Id,
                    byId,
                    visiting,
                    visited))
            {
                recursiveIds.Add(definition.Id);
            }
        }

        return recursiveIds;
    }

    private static bool ReferencesDefinition(
        BlockDefinitionId rootId,
        BlockDefinitionId currentId,
        IReadOnlyDictionary<BlockDefinitionId, BlockDefinition> definitionsById,
        ISet<BlockDefinitionId> visiting,
        ISet<BlockDefinitionId> visited)
    {
        if (!definitionsById.TryGetValue(currentId, out BlockDefinition? currentDefinition))
        {
            return false;
        }

        if (!visiting.Add(currentId))
        {
            return currentId == rootId;
        }

        foreach (BlockReferenceEntity reference in currentDefinition.Entities.OfType<BlockReferenceEntity>())
        {
            if (reference.BlockDefinitionId == rootId)
            {
                return true;
            }

            if (visited.Contains(reference.BlockDefinitionId))
            {
                continue;
            }

            if (ReferencesDefinition(
                    rootId,
                    reference.BlockDefinitionId,
                    definitionsById,
                    visiting,
                    visited))
            {
                return true;
            }
        }

        visiting.Remove(currentId);
        visited.Add(currentId);
        return false;
    }
}
