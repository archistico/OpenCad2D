namespace OpenCad2D.Persistence;

/// <summary>
/// Describes a single issue found while recovering a persisted document.
/// </summary>
public sealed class DocumentRecoveryIssue
{
    public DocumentRecoveryIssue(
        DocumentRecoverySeverity severity,
        string code,
        string message)
    {
        Severity = severity;
        Code = string.IsNullOrWhiteSpace(code) ? "UNKNOWN" : code;
        Message = string.IsNullOrWhiteSpace(message) ? "Unknown recovery issue." : message;
    }

    public DocumentRecoverySeverity Severity { get; }

    public string Code { get; }

    public string Message { get; }
}
