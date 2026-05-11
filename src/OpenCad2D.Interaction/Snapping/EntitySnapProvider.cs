using OpenCad2D.Interaction.HitTesting;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides an entity snap candidate for selection-oriented tools.
/// Unlike geometric snaps, this snap represents the selectable entity under the cursor.
/// </summary>
public sealed class EntitySnapProvider : ISnapProvider
{
    private readonly HitTestService _hitTestService;

    public EntitySnapProvider()
        : this(new HitTestService())
    {
    }

    public EntitySnapProvider(HitTestService hitTestService)
    {
        ArgumentNullException.ThrowIfNull(hitTestService);

        _hitTestService = hitTestService;
    }

    public SnapKind Kind => SnapKind.Entity;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<HitTestResult> results = _hitTestService.HitTestAll(
            request.Document,
            request.CursorPoint,
            request.Tolerance);

        foreach (HitTestResult result in results)
        {
            yield return new SnapCandidate(
                SnapKind.Entity,
                result.ClosestPoint,
                result.Entity.Id,
                result.Distance);
        }
    }
}
