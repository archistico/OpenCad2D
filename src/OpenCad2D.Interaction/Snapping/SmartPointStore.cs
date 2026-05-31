using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Stores the temporary smart points captured during the current command.
/// The store keeps a small bounded set so advanced tracking cannot clutter the viewport.
/// </summary>
public sealed class SmartPointStore
{
    private readonly List<SmartPoint> _points = new();

    public SmartPointStore(int maximumCount = 5)
    {
        if (maximumCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCount),
                "The maximum smart point count must be greater than zero.");
        }

        MaximumCount = maximumCount;
    }

    public int MaximumCount { get; }

    public IReadOnlyList<SmartPoint> Points => _points;

    public void AddOrRefresh(SmartPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        int existingIndex = _points.FindIndex(existing => IsSameReference(existing, point));

        if (existingIndex >= 0)
        {
            _points.RemoveAt(existingIndex);
        }

        _points.Add(point);

        while (_points.Count > MaximumCount)
        {
            _points.RemoveAt(0);
        }
    }

    public void Clear()
    {
        _points.Clear();
    }

    private static bool IsSameReference(
        SmartPoint left,
        SmartPoint right)
    {
        return left.Position == right.Position &&
               left.SourceSnapKind == right.SourceSnapKind &&
               Nullable.Equals(left.SourceEntityId, right.SourceEntityId);
    }
}
