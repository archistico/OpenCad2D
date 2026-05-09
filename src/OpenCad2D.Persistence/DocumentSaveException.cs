namespace OpenCad2D.Persistence;

/// <summary>
/// Exception thrown when a document cannot be saved.
/// </summary>
public class DocumentSaveException : Exception
{
    public DocumentSaveException(string message)
        : base(message)
    {
    }

    public DocumentSaveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
