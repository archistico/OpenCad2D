using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OpenCad2D.App.ViewModels.Properties;

public sealed class PropertyRowViewModel : INotifyPropertyChanged
{
    private string _editableValue;

    public PropertyRowViewModel(
        string name,
        string value)
        : this(
            name,
            value,
            isEditable: false,
            apply: null,
            options: null)
    {
    }

    public PropertyRowViewModel(
        string name,
        string value,
        bool isEditable,
        Action<string>? apply)
        : this(
            name,
            value,
            isEditable,
            apply,
            options: null)
    {
    }

    public PropertyRowViewModel(
        string name,
        string value,
        bool isEditable,
        Action<string>? apply,
        IEnumerable<string>? options)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Property name cannot be empty.",
                nameof(name));
        }

        Name = name;
        Value = value ?? string.Empty;
        _editableValue = Value;
        IsEditable = isEditable;
        Options = options?.ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();
        IsComboBox = IsEditable && Options.Count > 0;
        IsTextBox = IsEditable && !IsComboBox;
        ApplyCommand = new PropertyRowCommand(
            () => apply?.Invoke(EditableValue),
            () => IsEditable && apply is not null);
    }

    public string Name { get; }

    public string Value { get; }

    public bool IsEditable { get; }

    public bool IsTextBox { get; }

    public bool IsComboBox { get; }

    public IReadOnlyList<string> Options { get; }

    public string EditableValue
    {
        get => _editableValue;
        set
        {
            if (_editableValue == value)
            {
                return;
            }

            _editableValue = value;
            OnPropertyChanged();
        }
    }

    public ICommand ApplyCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private sealed class PropertyRowCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public PropertyRowCommand(
            Action execute,
            Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute();
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
