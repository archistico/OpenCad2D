using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Blocks;

public sealed class BlockManagerWindowViewModel : INotifyPropertyChanged
{
    private EditableBlockDefinitionViewModel? _selectedBlock;
    private string _validationMessage = string.Empty;

    public BlockManagerWindowViewModel(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Blocks = new ObservableCollection<EditableBlockDefinitionViewModel>(
            document.BlockDefinitions.All
                .OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
                .Select(definition => new EditableBlockDefinitionViewModel(
                    definition,
                    document.Entities.All
                        .OfType<BlockReferenceEntity>()
                        .Count(reference => reference.BlockDefinitionId == definition.Id))));

        SelectedBlock = Blocks.FirstOrDefault();
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
            OnPropertyChanged(nameof(HasSelectedBlock));
            OnPropertyChanged(nameof(CanDeleteSelectedBlock));
            OnPropertyChanged(nameof(CanInsertSelectedBlock));
        }
    }

    public bool HasSelectedBlock => SelectedBlock is not null;

    public bool CanDeleteSelectedBlock => SelectedBlock?.CanDelete == true;

    public bool CanInsertSelectedBlock => SelectedBlock is not null;

    public string SummaryText => Blocks.Count == 1
        ? "1 block definition"
        : $"{Blocks.Count} block definitions";

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

    public void DeleteSelectedBlock()
    {
        if (SelectedBlock is null)
        {
            return;
        }

        if (!SelectedBlock.CanDelete)
        {
            ValidationMessage = "The selected block cannot be deleted because it still has instances in the drawing.";
            return;
        }

        Blocks.Remove(SelectedBlock);
        SelectedBlock = Blocks.FirstOrDefault();
        ClearValidation();
        OnPropertyChanged(nameof(SummaryText));
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

        foreach (EditableBlockDefinitionViewModel block in Blocks)
        {
            string? validation = block.Validate();

            if (validation is not null)
            {
                ValidationMessage = validation;
                SelectedBlock = block;
                return false;
            }
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

        if (action == BlockManagerAction.InsertSelected && SelectedBlock is null)
        {
            ValidationMessage = "Select a block definition before inserting it.";
            return false;
        }

        result = new BlockManagerResult(
            action,
            Blocks.Select(block => block.ToBlockDefinition()).ToList(),
            SelectedBlock?.Id,
            SelectedBlock?.Name.Trim());

        ClearValidation();
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
}
