using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels.Blocks;

public sealed class EditableBlockDefinitionViewModel : INotifyPropertyChanged
{
    private string _name;

    public EditableBlockDefinitionViewModel(
        BlockDefinition definition,
        int instanceCount,
        int nestedReferenceCount = 0,
        int missingNestedReferenceCount = 0,
        bool containsNestedBlockReferences = false,
        bool hasSelfReference = false,
        bool hasRecursiveReference = false,
        bool isReachableFromDrawing = false)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        Id = definition.Id;
        _name = definition.Name;
        EntityCount = definition.Entities.Count;
        InstanceCount = instanceCount;
        NestedReferenceCount = nestedReferenceCount;
        MissingNestedReferenceCount = missingNestedReferenceCount;
        ContainsNestedBlockReferences = containsNestedBlockReferences;
        HasSelfReference = hasSelfReference;
        HasRecursiveReference = hasRecursiveReference;
        IsReachableFromDrawing = isReachableFromDrawing;
        IsEmpty = definition.IsEmpty;
        BoundsText = FormatBounds(definition.GetBoundingBox());
    }

    public BlockDefinition Definition { get; }

    public BlockDefinitionId Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            string nextValue = value ?? string.Empty;

            if (_name == nextValue)
            {
                return;
            }

            _name = nextValue;
            OnPropertyChanged();
        }
    }

    public int EntityCount { get; }

    public int InstanceCount { get; }

    public int NestedReferenceCount { get; }

    public int TotalReferenceCount => InstanceCount + NestedReferenceCount;

    public int MissingNestedReferenceCount { get; }

    public bool ContainsNestedBlockReferences { get; }

    public bool HasSelfReference { get; }

    public bool HasRecursiveReference { get; }

    public bool IsReachableFromDrawing { get; }

    public bool IsEmpty { get; }

    public string EntityCountText => EntityCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string InstanceCountText => InstanceCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string NestedReferenceCountText => NestedReferenceCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string TotalReferenceCountText => TotalReferenceCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string BoundsText { get; }

    public bool HasDiagnosticIssue => IsEmpty ||
                                      MissingNestedReferenceCount > 0 ||
                                      HasRecursiveReference;

    public bool HasBlockingDiagnostic => MissingNestedReferenceCount > 0 ||
                                         HasRecursiveReference;

    public bool CanDelete => TotalReferenceCount == 0 && !HasBlockingDiagnostic;

    public string StatusText
    {
        get
        {
            if (HasRecursiveReference)
            {
                return "Recursive";
            }

            if (MissingNestedReferenceCount > 0)
            {
                return "Invalid";
            }

            if (IsEmpty)
            {
                return "Empty";
            }

            return IsReachableFromDrawing
                ? "Used"
                : "Unused";
        }
    }

    public string DiagnosticText
    {
        get
        {
            var parts = new List<string>();

            if (IsEmpty)
            {
                parts.Add("empty definition");
            }

            if (HasRecursiveReference)
            {
                parts.Add(HasSelfReference
                    ? "self-referencing block"
                    : "recursive nested block reference");
            }

            if (MissingNestedReferenceCount > 0)
            {
                parts.Add(MissingNestedReferenceCount == 1
                    ? "1 missing nested reference"
                    : $"{MissingNestedReferenceCount} missing nested references");
            }

            if (parts.Count == 0)
            {
                return ContainsNestedBlockReferences
                    ? "No blocking diagnostics. Contains nested block references."
                    : "No diagnostics.";
            }

            return string.Join("; ", parts) + ".";
        }
    }

    public string DetailsText =>
        $"{Name.Trim()} · {EntityCountText} entities · {InstanceCountText} drawing refs · " +
        $"{NestedReferenceCountText} nested refs · {TotalReferenceCountText} total refs · " +
        $"{(IsReachableFromDrawing ? "reachable from drawing" : "not drawing-reachable")} · " +
        $"bounds {BoundsText}. {DiagnosticText}";

    public string? Validate(bool allowBlockingDiagnostics = false)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Block name cannot be empty.";
        }

        if (!allowBlockingDiagnostics)
        {
            if (HasRecursiveReference)
            {
                return $"Block '{Name.Trim()}' contains a recursive block reference.";
            }

            if (MissingNestedReferenceCount > 0)
            {
                return $"Block '{Name.Trim()}' contains nested references to missing block definitions.";
            }
        }

        return null;
    }

    public BlockDefinition ToBlockDefinition()
    {
        return Definition.WithName(Name.Trim());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatBounds(BoundingBox2D bounds)
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{bounds.Width:0.###} × {bounds.Height:0.###}");
    }
}
