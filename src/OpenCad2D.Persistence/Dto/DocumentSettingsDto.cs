namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable document-level settings.
/// </summary>
public sealed class DocumentSettingsDto
{
    public string CurrentLayerId { get; set; } = "0";
}
