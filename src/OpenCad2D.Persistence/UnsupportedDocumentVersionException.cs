namespace OpenCad2D.Persistence;

/// <summary>
/// Exception thrown when a document uses an unsupported file format version.
/// </summary>
public sealed class UnsupportedDocumentVersionException : DocumentLoadException
{
    public UnsupportedDocumentVersionException(int version)
        : base($"Unsupported OpenCad2D document version: {version}.")
    {
        Version = version;
    }

    public int Version { get; }
}
