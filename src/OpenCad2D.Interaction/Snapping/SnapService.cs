namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Coordinates snap providers and returns the best snap candidate.
/// </summary>
public sealed class SnapService
{
    private readonly IReadOnlyList<ISnapProvider> _providers;

    public SnapService()
    : this(new ISnapProvider[]
    {
        new EndpointSnapProvider(),
        new MidpointSnapProvider(),
        new CenterSnapProvider(),
        new QuadrantSnapProvider(),
        new IntersectionSnapProvider(),
        new PerpendicularSnapProvider(),
        new TangentSnapProvider(),
        new NearestSnapProvider(),
        new GridSnapProvider(),
        new EntitySnapProvider()
    })
    {
    }

    public SnapService(IEnumerable<ISnapProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        _providers = providers.ToList();
    }

    public SnapCandidate? Snap(SnapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return GetCandidates(request)
            .OrderBy(candidate => GetPriority(candidate.Kind))
            .ThenBy(candidate => candidate.DistanceToCursor)
            .FirstOrDefault();
    }

    public IReadOnlyList<SnapCandidate> GetCandidates(SnapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new List<SnapCandidate>();

        foreach (ISnapProvider provider in _providers)
        {
            if (!request.IsEnabled(provider.Kind))
            {
                continue;
            }

            result.AddRange(provider.GetCandidates(request));
        }

        return result;
    }

    private static int GetPriority(SnapKind kind)
    {
        return kind switch
        {
            SnapKind.Entity => -100,
            SnapKind.Endpoint => 0,
            SnapKind.Intersection => 1,
            SnapKind.Center => 2,
            SnapKind.Midpoint => 3,
            SnapKind.Perpendicular => 4,
            SnapKind.Quadrant => 5,
            SnapKind.Tangent => 6,
            SnapKind.Nearest => 100,
            SnapKind.Grid => 200,
            _ => 1000
        };
    }
}