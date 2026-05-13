namespace OpenCad2D.Export.Dxf.Import;

/// <summary>
/// Exception raised when an ASCII DXF stream cannot be read as group-code pairs.
/// </summary>
public sealed class DxfReadException : Exception
{
    public DxfReadException(string message)
        : base(message)
    {
    }
}
