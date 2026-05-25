using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence;

/// <summary>
/// Resolves and normalizes paths for externally linked resources stored in an OpenCad2D document.
/// </summary>
internal static class ExternalReferencePathHelper
{
    public static void MakeImageReferencePathsRelativeToDocument(
        DocumentDto dto,
        string documentFilePath)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string? documentDirectory = Path.GetDirectoryName(documentFilePath);

        if (string.IsNullOrWhiteSpace(documentDirectory))
        {
            return;
        }

        foreach (ImageReferenceEntityDto imageReference in GetImageReferenceDtos(dto))
        {
            imageReference.FilePath = TryMakeRelativePath(
                documentDirectory,
                imageReference.FilePath);
        }
    }

    public static void ResolveImageReferencePathsAgainstDocument(
        DocumentDto dto,
        string documentFilePath)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string? documentDirectory = Path.GetDirectoryName(documentFilePath);

        if (string.IsNullOrWhiteSpace(documentDirectory))
        {
            return;
        }

        foreach (ImageReferenceEntityDto imageReference in GetImageReferenceDtos(dto))
        {
            imageReference.FilePath = TryResolvePath(
                documentDirectory,
                imageReference.FilePath);
        }
    }

    private static IEnumerable<ImageReferenceEntityDto> GetImageReferenceDtos(DocumentDto dto)
    {
        return dto.Entities
            .OfType<ImageReferenceEntityDto>()
            .Where(imageReference => !string.IsNullOrWhiteSpace(imageReference.FilePath));
    }

    private static string TryMakeRelativePath(
        string documentDirectory,
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !Path.IsPathFullyQualified(filePath))
        {
            return filePath;
        }

        try
        {
            string normalizedDocumentDirectory = Path.GetFullPath(documentDirectory);
            string normalizedFilePath = Path.GetFullPath(filePath);
            string relativePath = Path.GetRelativePath(
                normalizedDocumentDirectory,
                normalizedFilePath);

            return string.IsNullOrWhiteSpace(relativePath)
                ? filePath
                : relativePath;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return filePath;
        }
    }

    private static string TryResolvePath(
        string documentDirectory,
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || Path.IsPathFullyQualified(filePath))
        {
            return filePath;
        }

        try
        {
            return Path.GetFullPath(Path.Combine(documentDirectory, filePath));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return filePath;
        }
    }
}
