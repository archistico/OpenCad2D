namespace OpenCad2D.Core.Entities;

/// <summary>
/// Represents an entity that can use a layer-provided solid fill.
/// </summary>
public interface IFillableEntity
{
    bool IsFilled { get; }

    CadEntity WithFill(bool isFilled);
}
