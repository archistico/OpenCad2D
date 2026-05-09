using System.Text.Json;

namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// DTO used when a file contains an entity type that this version cannot load.
/// </summary>
public sealed class UnknownEntityDto : EntityDto
{
    public JsonElement RawJson { get; set; }
}
