namespace OpenCad2D.Persistence;

/// <summary>
/// Base exception for document loading failures.
/// </summary>
public class DocumentLoadException : Exception
{
    public DocumentLoadException(string message)
        : base(message)
    {
    }

    public DocumentLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
