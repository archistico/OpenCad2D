namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Base DTO for persisted CAD entities.
/// </summary>
public abstract class EntityDto
{
    public string Type { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string LayerId { get; set; } = "0";
}
