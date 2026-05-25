using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence;

/// <summary>
/// Reads and writes polymorphic entity DTOs using the v1 'type' discriminator.
/// </summary>
public sealed class EntityDtoJsonConverter : JsonConverter<EntityDto>
{
    public override EntityDto? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement.Clone();

        string? type = TryGetType(root);

        EntityDto? result = type switch
        {
            EntityTypeNames.Point => root.Deserialize<PointEntityDto>(options),
            EntityTypeNames.Text => root.Deserialize<TextEntityDto>(options),
            EntityTypeNames.MultilineText => root.Deserialize<MultilineTextEntityDto>(options),
            EntityTypeNames.LinearDimension => root.Deserialize<LinearDimensionEntityDto>(options),
            EntityTypeNames.AlignedDimension => root.Deserialize<AlignedDimensionEntityDto>(options),
            EntityTypeNames.RadiusDimension => root.Deserialize<RadiusDimensionEntityDto>(options),
            EntityTypeNames.DiameterDimension => root.Deserialize<DiameterDimensionEntityDto>(options),
            EntityTypeNames.AngularDimension => root.Deserialize<AngularDimensionEntityDto>(options),
            EntityTypeNames.Line => root.Deserialize<LineEntityDto>(options),
            EntityTypeNames.Circle => root.Deserialize<CircleEntityDto>(options),
            EntityTypeNames.Ellipse => root.Deserialize<EllipseEntityDto>(options),
            EntityTypeNames.EllipticalArc => root.Deserialize<EllipticalArcEntityDto>(options),
            EntityTypeNames.Arc => root.Deserialize<ArcEntityDto>(options),
            EntityTypeNames.Polyline => root.Deserialize<PolylineEntityDto>(options),
            EntityTypeNames.BezierSpline => root.Deserialize<BezierSplineEntityDto>(options),
            EntityTypeNames.ImageReference => root.Deserialize<ImageReferenceEntityDto>(options),
            _ => new UnknownEntityDto
            {
                Type = type ?? string.Empty,
                Id = TryGetString(root, "id") ?? string.Empty,
                LayerId = TryGetString(root, "layerId") ?? "0",
                RawJson = root
            }
        };

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        EntityDto value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case PointEntityDto point:
                JsonSerializer.Serialize(writer, point, options);
                break;

            case TextEntityDto text:
                JsonSerializer.Serialize(writer, text, options);
                break;

            case MultilineTextEntityDto multilineText:
                JsonSerializer.Serialize(writer, multilineText, options);
                break;

            case LinearDimensionEntityDto linearDimension:
                JsonSerializer.Serialize(writer, linearDimension, options);
                break;

            case AlignedDimensionEntityDto alignedDimension:
                JsonSerializer.Serialize(writer, alignedDimension, options);
                break;

            case RadiusDimensionEntityDto radiusDimension:
                JsonSerializer.Serialize(writer, radiusDimension, options);
                break;

            case DiameterDimensionEntityDto diameterDimension:
                JsonSerializer.Serialize(writer, diameterDimension, options);
                break;

            case AngularDimensionEntityDto angularDimension:
                JsonSerializer.Serialize(writer, angularDimension, options);
                break;

            case LineEntityDto line:
                JsonSerializer.Serialize(writer, line, options);
                break;

            case CircleEntityDto circle:
                JsonSerializer.Serialize(writer, circle, options);
                break;

            case EllipseEntityDto ellipse:
                JsonSerializer.Serialize(writer, ellipse, options);
                break;

            case EllipticalArcEntityDto ellipticalArc:
                JsonSerializer.Serialize(writer, ellipticalArc, options);
                break;

            case ArcEntityDto arc:
                JsonSerializer.Serialize(writer, arc, options);
                break;

            case PolylineEntityDto polyline:
                JsonSerializer.Serialize(writer, polyline, options);
                break;

            case BezierSplineEntityDto spline:
                JsonSerializer.Serialize(writer, spline, options);
                break;

            case ImageReferenceEntityDto imageReference:
                JsonSerializer.Serialize(writer, imageReference, options);
                break;

            case UnknownEntityDto unknown:
                unknown.RawJson.WriteTo(writer);
                break;

            default:
                throw new NotSupportedException(
                    $"Unsupported entity DTO type '{value.GetType().Name}'.");
        }
    }

    private static string? TryGetType(JsonElement root)
    {
        return TryGetString(root, "type") ??
               TryGetString(root, "Type");
    }

    private static string? TryGetString(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }
}
