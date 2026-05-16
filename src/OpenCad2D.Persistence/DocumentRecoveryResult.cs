using OpenCad2D.Core.Documents;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence;

/// <summary>
/// Result of a tolerant document recovery pass.
/// </summary>
public sealed class DocumentRecoveryResult
{
    public DocumentRecoveryResult(
        CadDocument document,
        string currentLayerId,
        ViewportStateDto viewport,
        IReadOnlyList<DocumentRecoveryIssue> issues,
        int recoveredEntityCount,
        int skippedEntityCount)
    {
        Document = document;
        CurrentLayerId = currentLayerId;
        Viewport = viewport;
        Issues = issues;
        RecoveredEntityCount = recoveredEntityCount;
        SkippedEntityCount = skippedEntityCount;
    }

    public CadDocument Document { get; }

    public string CurrentLayerId { get; }

    public ViewportStateDto Viewport { get; }

    public IReadOnlyList<DocumentRecoveryIssue> Issues { get; }

    public int RecoveredEntityCount { get; }

    public int SkippedEntityCount { get; }

    public bool HasIssues => Issues.Count > 0;
}
