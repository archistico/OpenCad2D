namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable reusable block definition.
/// </summary>
public sealed class BlockDefinitionDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<EntityDto> Entities { get; set; } = new();
}
