namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides snap candidates for a given snap request.
/// </summary>
public interface ISnapProvider
{
    SnapKind Kind { get; }

    IEnumerable<SnapCandidate> GetCandidates(SnapRequest request);
}