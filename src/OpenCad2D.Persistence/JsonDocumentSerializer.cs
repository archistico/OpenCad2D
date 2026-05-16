using System.Globalization;
using System.Text.Json;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence;

/// <summary>
/// JSON serializer for the internal OpenCad2D v1 file format.
/// </summary>
public sealed class JsonDocumentSerializer : IDocumentSerializer
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public DocumentDto Serialize(
        CadDocument document,
        string currentLayerId,
        ViewportStateDto viewport,
        DocumentSettingsDto? settings = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(viewport);

        return new DocumentDto
        {
            Version = CurrentVersion,
            SavedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Settings = NormalizeSettings(settings, currentLayerId),
            Viewport = new ViewportStateDto
            {
                PanX = viewport.PanX,
                PanY = viewport.PanY,
                Zoom = viewport.Zoom
            },
            LineFormats = document.LineFormats.All
                .Select(ToDto)
                .ToList(),
            TextFormats = document.TextFormats.All
                .Select(ToDto)
                .ToList(),
            DimensionStyles = document.DimensionStyles.All
                .Select(ToDto)
                .ToList(),
            Layers = document.Layers.All
                .Select(ToDto)
                .ToList(),
            Entities = document.Entities.All
                .Select(ToDto)
                .Where(dto => dto is not null)
                .Cast<EntityDto>()
                .ToList()
        };
    }

    public CadDocument Deserialize(
        DocumentDto dto,
        out string currentLayerId,
        out ViewportStateDto viewport)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Version != CurrentVersion)
        {
            throw new UnsupportedDocumentVersionException(dto.Version);
        }

        dto.Settings ??= new DocumentSettingsDto();

        CadDocument document = new();
        LineFormatCollection lineFormats = FromLineFormatDtos(dto.LineFormats);
        document.ReplaceLineFormats(lineFormats);

        TextFormatCollection textFormats = FromTextFormatDtos(dto.TextFormats);
        document.ReplaceTextFormats(textFormats);

        DimensionStyleCollection dimensionStyles = FromDimensionStyleDtos(dto.DimensionStyles);
        document.ReplaceDimensionStyles(dimensionStyles);

        foreach (LayerDto layerDto in dto.Layers)
        {
            Layer layer = FromDto(layerDto, lineFormats);

            if (document.Layers.Contains(layer.Id))
            {
                document.Layers.Replace(layer);
            }
            else
            {
                document.Layers.Add(layer);
            }
        }

        foreach (EntityDto entityDto in dto.Entities)
        {
            CadEntity? entity = FromDto(entityDto);

            if (entity is null)
            {
                continue;
            }

            if (!document.Layers.Contains(entity.LayerId))
            {
                continue;
            }

            document.AddEntity(entity);
        }

        currentLayerId = string.IsNullOrWhiteSpace(dto.Settings.CurrentLayerId)
            ? LayerId.Default.Value
            : dto.Settings.CurrentLayerId;

        if (!document.Layers.Contains(new LayerId(currentLayerId)))
        {
            currentLayerId = LayerId.Default.Value;
        }

        viewport = dto.Viewport ?? new ViewportStateDto();

        return document;
    }

    public DocumentRecoveryResult DeserializeWithRecovery(DocumentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Version != CurrentVersion)
        {
            throw new UnsupportedDocumentVersionException(dto.Version);
        }

        dto.Settings ??= new DocumentSettingsDto();

        var issues = new List<DocumentRecoveryIssue>();
        CadDocument document = new();

        LineFormatCollection lineFormats = FromLineFormatDtos(dto.LineFormats);
        document.ReplaceLineFormats(lineFormats);

        TextFormatCollection textFormats = FromTextFormatDtos(dto.TextFormats);
        document.ReplaceTextFormats(textFormats);

        DimensionStyleCollection dimensionStyles = FromDimensionStyleDtos(dto.DimensionStyles);
        document.ReplaceDimensionStyles(dimensionStyles);

        foreach (LayerDto layerDto in dto.Layers)
        {
            try
            {
                Layer layer = FromDto(layerDto, lineFormats);

                if (document.Layers.Contains(layer.Id))
                {
                    document.Layers.Replace(layer);
                }
                else
                {
                    document.Layers.Add(layer);
                }
            }
            catch (DocumentLoadException exception)
            {
                issues.Add(new DocumentRecoveryIssue(
                    DocumentRecoverySeverity.Warning,
                    "LAYER_SKIPPED",
                    exception.Message));
            }
        }

        int recoveredEntityCount = 0;
        int skippedEntityCount = 0;

        foreach (EntityDto entityDto in dto.Entities)
        {
            CadEntity? entity;

            try
            {
                entity = FromDto(entityDto);
            }
            catch (DocumentLoadException exception)
            {
                skippedEntityCount++;
                issues.Add(new DocumentRecoveryIssue(
                    DocumentRecoverySeverity.Warning,
                    "ENTITY_SKIPPED",
                    exception.Message));
                continue;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                skippedEntityCount++;
                issues.Add(new DocumentRecoveryIssue(
                    DocumentRecoverySeverity.Warning,
                    "ENTITY_SKIPPED",
                    $"Skipped invalid entity '{entityDto.Id}': {exception.Message}"));
                continue;
            }

            if (entity is null)
            {
                skippedEntityCount++;
                issues.Add(new DocumentRecoveryIssue(
                    DocumentRecoverySeverity.Warning,
                    "ENTITY_SKIPPED",
                    $"Skipped unsupported or incomplete entity '{entityDto.Id}'."));
                continue;
            }

            if (!document.Layers.Contains(entity.LayerId))
            {
                issues.Add(new DocumentRecoveryIssue(
                    DocumentRecoverySeverity.Warning,
                    "ENTITY_LAYER_REPAIRED",
                    $"Entity '{entity.Id}' referenced missing layer '{entity.LayerId}' and was moved to layer '{LayerId.Default.Value}'."));

                entity = entity.WithLayer(LayerId.Default);
            }

            try
            {
                document.AddEntity(entity);
                recoveredEntityCount++;
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                skippedEntityCount++;
                issues.Add(new DocumentRecoveryIssue(
                    DocumentRecoverySeverity.Warning,
                    "ENTITY_SKIPPED",
                    $"Skipped entity '{entity.Id}': {exception.Message}"));
            }
        }

        string currentLayerId = string.IsNullOrWhiteSpace(dto.Settings.CurrentLayerId)
            ? LayerId.Default.Value
            : dto.Settings.CurrentLayerId;

        if (!document.Layers.Contains(new LayerId(currentLayerId)))
        {
            issues.Add(new DocumentRecoveryIssue(
                DocumentRecoverySeverity.Warning,
                "CURRENT_LAYER_REPAIRED",
                $"Current layer '{currentLayerId}' was not found and was reset to '{LayerId.Default.Value}'."));

            currentLayerId = LayerId.Default.Value;
        }

        ViewportStateDto viewport = dto.Viewport ?? new ViewportStateDto();

        return new DocumentRecoveryResult(
            document,
            currentLayerId,
            viewport,
            dto.Settings,
            issues,
            recoveredEntityCount,
            skippedEntityCount);
    }

    public void SaveToFile(
        DocumentDto dto,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(filePath));
        }

        try
        {
            string? directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(dto, JsonOptions);

            File.WriteAllText(filePath, json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
        {
            throw new DocumentSaveException(
                $"Cannot save OpenCad2D document to '{filePath}'.",
                exception);
        }
    }

    public DocumentDto LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(filePath));
        }

        try
        {
            string json = File.ReadAllText(filePath);

            DocumentDto? dto = JsonSerializer.Deserialize<DocumentDto>(json, JsonOptions);

            if (dto is null)
            {
                throw new DocumentLoadException(
                    $"File '{filePath}' does not contain a valid OpenCad2D document.");
            }

            if (dto.Version != CurrentVersion)
            {
                throw new UnsupportedDocumentVersionException(dto.Version);
            }

            return dto;
        }
        catch (UnsupportedDocumentVersionException)
        {
            throw;
        }
        catch (DocumentLoadException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            throw new DocumentLoadException(
                $"Cannot load OpenCad2D document from '{filePath}'.",
                exception);
        }
    }

    public static string ToJson(DocumentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static DocumentDto FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException(
                "JSON cannot be empty.",
                nameof(json));
        }

        try
        {
            DocumentDto? dto = JsonSerializer.Deserialize<DocumentDto>(json, JsonOptions);

            if (dto is null)
            {
                throw new DocumentLoadException(
                    "JSON does not contain a valid OpenCad2D document.");
            }

            if (dto.Version != CurrentVersion)
            {
                throw new UnsupportedDocumentVersionException(dto.Version);
            }

            return dto;
        }
        catch (UnsupportedDocumentVersionException)
        {
            throw;
        }
        catch (DocumentLoadException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new DocumentLoadException(
                "Cannot load OpenCad2D document from JSON.",
                exception);
        }
    }

    private static LineFormatDto ToDto(LineFormat format)
    {
        return new LineFormatDto
        {
            Id = format.Id.Value,
            Name = format.Name,
            Color = ToHex(format.Color),
            LineWeight = format.LineWeight.Millimeters,
            LineStyle = format.LineStyle.ToString(),
            DashPattern = format.DashPattern.ToList()
        };
    }

    private static TextFormatDto ToDto(TextFormat format)
    {
        return new TextFormatDto
        {
            Id = format.Id.Value,
            Name = format.Name,
            FontFamily = format.FontFamily,
            Height = format.Height,
            Color = ToHex(format.Color),
            IsBold = format.IsBold,
            IsItalic = format.IsItalic
        };
    }

    private static DimensionStyleDto ToDto(DimensionStyle style)
    {
        return new DimensionStyleDto
        {
            Id = style.Id.Value,
            Name = style.Name,
            TextFormatId = style.TextFormatId.Value,
            ArrowSize = style.ArrowSize,
            TextOffset = style.TextOffset,
            ExtensionLineOffset = style.ExtensionLineOffset,
            ExtensionLineOvershoot = style.ExtensionLineOvershoot,
            DecimalPlaces = style.DecimalPlaces,
            DecimalSeparator = style.DecimalSeparator,
            Suffix = style.Suffix
        };
    }

    private static LayerDto ToDto(Layer layer)
    {
        return new LayerDto
        {
            Id = layer.Id.Value,
            Name = layer.Name,
            LineFormatId = layer.LineFormatId.Value,
            IsVisible = layer.IsVisible,
            IsLocked = layer.IsLocked
        };
    }

    private static EntityDto? ToDto(CadEntity entity)
    {
        return entity switch
        {
            PointEntity point => new PointEntityDto
            {
                Id = point.Id.ToString(),
                LayerId = point.LayerId.Value,
                X = point.Position.X,
                Y = point.Position.Y
            },

            TextEntity text => new TextEntityDto
            {
                Id = text.Id.ToString(),
                LayerId = text.LayerId.Value,
                Text = text.Text,
                InsertionX = text.InsertionPoint.X,
                InsertionY = text.InsertionPoint.Y,
                RotationDegrees = text.RotationDegrees,
                TextFormatId = text.TextFormatId.Value
            },

            MultilineTextEntity multilineText => new MultilineTextEntityDto
            {
                Id = multilineText.Id.ToString(),
                LayerId = multilineText.LayerId.Value,
                Text = multilineText.Text,
                InsertionX = multilineText.InsertionPoint.X,
                InsertionY = multilineText.InsertionPoint.Y,
                RotationDegrees = multilineText.RotationDegrees,
                TextFormatId = multilineText.TextFormatId.Value
            },

            LinearDimensionEntity linearDimension => new LinearDimensionEntityDto
            {
                Id = linearDimension.Id.ToString(),
                LayerId = linearDimension.LayerId.Value,
                FirstX = linearDimension.FirstPoint.X,
                FirstY = linearDimension.FirstPoint.Y,
                SecondX = linearDimension.SecondPoint.X,
                SecondY = linearDimension.SecondPoint.Y,
                DimensionLineX = linearDimension.DimensionLinePoint.X,
                DimensionLineY = linearDimension.DimensionLinePoint.Y,
                Orientation = linearDimension.Orientation.ToString(),
                DimensionStyleId = linearDimension.DimensionStyleId.Value,
                TextOverride = linearDimension.TextOverride,
                IsStale = linearDimension.IsStale
            },

            AlignedDimensionEntity alignedDimension => new AlignedDimensionEntityDto
            {
                Id = alignedDimension.Id.ToString(),
                LayerId = alignedDimension.LayerId.Value,
                FirstX = alignedDimension.FirstPoint.X,
                FirstY = alignedDimension.FirstPoint.Y,
                SecondX = alignedDimension.SecondPoint.X,
                SecondY = alignedDimension.SecondPoint.Y,
                DimensionLineX = alignedDimension.DimensionLinePoint.X,
                DimensionLineY = alignedDimension.DimensionLinePoint.Y,
                DimensionStyleId = alignedDimension.DimensionStyleId.Value,
                TextOverride = alignedDimension.TextOverride,
                IsStale = alignedDimension.IsStale
            },

            RadiusDimensionEntity radiusDimension => new RadiusDimensionEntityDto
            {
                Id = radiusDimension.Id.ToString(),
                LayerId = radiusDimension.LayerId.Value,
                CenterX = radiusDimension.Center.X,
                CenterY = radiusDimension.Center.Y,
                PointOnCircleX = radiusDimension.PointOnCircle.X,
                PointOnCircleY = radiusDimension.PointOnCircle.Y,
                TextX = radiusDimension.TextPoint.X,
                TextY = radiusDimension.TextPoint.Y,
                DimensionStyleId = radiusDimension.DimensionStyleId.Value,
                TextOverride = radiusDimension.TextOverride,
                IsStale = radiusDimension.IsStale
            },

            DiameterDimensionEntity diameterDimension => new DiameterDimensionEntityDto
            {
                Id = diameterDimension.Id.ToString(),
                LayerId = diameterDimension.LayerId.Value,
                CenterX = diameterDimension.Center.X,
                CenterY = diameterDimension.Center.Y,
                PointOnCircleX = diameterDimension.PointOnCircle.X,
                PointOnCircleY = diameterDimension.PointOnCircle.Y,
                TextX = diameterDimension.TextPoint.X,
                TextY = diameterDimension.TextPoint.Y,
                DimensionStyleId = diameterDimension.DimensionStyleId.Value,
                TextOverride = diameterDimension.TextOverride,
                IsStale = diameterDimension.IsStale
            },

            AngularDimensionEntity angularDimension => new AngularDimensionEntityDto
            {
                Id = angularDimension.Id.ToString(),
                LayerId = angularDimension.LayerId.Value,
                CenterX = angularDimension.Center.X,
                CenterY = angularDimension.Center.Y,
                FirstRayX = angularDimension.FirstRayPoint.X,
                FirstRayY = angularDimension.FirstRayPoint.Y,
                SecondRayX = angularDimension.SecondRayPoint.X,
                SecondRayY = angularDimension.SecondRayPoint.Y,
                ArcX = angularDimension.ArcPoint.X,
                ArcY = angularDimension.ArcPoint.Y,
                IsCounterClockwise = angularDimension.IsCounterClockwise,
                DimensionStyleId = angularDimension.DimensionStyleId.Value,
                TextOverride = angularDimension.TextOverride,
                IsStale = angularDimension.IsStale
            },

            LineEntity line => new LineEntityDto
            {
                Id = line.Id.ToString(),
                LayerId = line.LayerId.Value,
                StartX = line.Start.X,
                StartY = line.Start.Y,
                EndX = line.End.X,
                EndY = line.End.Y
            },

            CircleEntity circle => new CircleEntityDto
            {
                Id = circle.Id.ToString(),
                LayerId = circle.LayerId.Value,
                CenterX = circle.Center.X,
                CenterY = circle.Center.Y,
                Radius = circle.Radius
            },

            EllipseEntity ellipse => new EllipseEntityDto
            {
                Id = ellipse.Id.ToString(),
                LayerId = ellipse.LayerId.Value,
                CenterX = ellipse.Center.X,
                CenterY = ellipse.Center.Y,
                MajorAxisX = ellipse.MajorAxis.X,
                MajorAxisY = ellipse.MajorAxis.Y,
                MinorRadius = ellipse.MinorRadius
            },

            ArcEntity arc => new ArcEntityDto
            {
                Id = arc.Id.ToString(),
                LayerId = arc.LayerId.Value,
                CenterX = arc.Center.X,
                CenterY = arc.Center.Y,
                Radius = arc.Radius,
                StartAngleDegrees = arc.StartAngle.Degrees,
                EndAngleDegrees = arc.EndAngle.Degrees,
                IsCounterClockwise = arc.IsCounterClockwise
            },

            PolylineEntity polyline => new PolylineEntityDto
            {
                Id = polyline.Id.ToString(),
                LayerId = polyline.LayerId.Value,
                IsClosed = polyline.IsClosed,
                Vertices = polyline.Vertices
                    .Select(vertex => new PointDto
                    {
                        X = vertex.X,
                        Y = vertex.Y
                    })
                    .ToList()
            },

            BezierSplineEntity spline => new BezierSplineEntityDto
            {
                Id = spline.Id.ToString(),
                LayerId = spline.LayerId.Value,
                IsClosed = spline.IsClosed,
                ControlPoints = spline.ControlPoints
                    .Select(point => new PointDto
                    {
                        X = point.X,
                        Y = point.Y
                    })
                    .ToList()
            },

            _ => null
        };
    }

    private static LineFormatCollection FromLineFormatDtos(IReadOnlyCollection<LineFormatDto>? dtos)
    {
        if (dtos is null || dtos.Count == 0)
        {
            return LineFormatCollection.Default;
        }

        var formats = new List<LineFormat>();

        foreach (LineFormatDto dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(dto.Name)
                ? dto.Id
                : dto.Name;

            LineStyle lineStyle = ParseLineStyle(dto.LineStyle);
            IReadOnlyList<double>? dashPattern = NormalizeDashPatternForLoad(
                lineStyle,
                dto.DashPattern);

            formats.Add(new LineFormat(
                new LineFormatId(dto.Id),
                name,
                FromHex(dto.Color),
                LineWeight.FromMillimeters(Math.Max(0, dto.LineWeight)),
                lineStyle,
                dashPattern));
        }

        if (formats.Count == 0)
        {
            return LineFormatCollection.Default;
        }

        if (!formats.Any(format => format.Id == LineFormatId.Continuous))
        {
            formats.Insert(
                0,
                LineFormatCollection.Default.GetById(LineFormatId.Continuous));
        }

        return new LineFormatCollection(formats);
    }


    private static IReadOnlyList<double>? NormalizeDashPatternForLoad(
        LineStyle lineStyle,
        IReadOnlyList<double>? dashPattern)
    {
        if (dashPattern is null || dashPattern.Count == 0)
        {
            return LineStyleDashPattern.Get(lineStyle);
        }

        if (!LineStyleDashPattern.IsValid(dashPattern))
        {
            return LineStyleDashPattern.Get(lineStyle);
        }

        return dashPattern;
    }

    private static TextFormatCollection FromTextFormatDtos(IReadOnlyCollection<TextFormatDto>? dtos)
    {
        if (dtos is null || dtos.Count == 0)
        {
            return TextFormatCollection.Default;
        }

        var formats = new List<TextFormat>();

        foreach (TextFormatDto dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(dto.Name)
                ? dto.Id
                : dto.Name;

            string fontFamily = string.IsNullOrWhiteSpace(dto.FontFamily)
                ? "Arial"
                : dto.FontFamily;

            formats.Add(new TextFormat(
                new TextFormatId(dto.Id),
                name,
                fontFamily,
                dto.Height <= 0 ? 10.0 : dto.Height,
                FromHex(dto.Color),
                dto.IsBold,
                dto.IsItalic));
        }

        if (formats.Count == 0)
        {
            return TextFormatCollection.Default;
        }

        if (!formats.Any(format => format.Id == TextFormatId.Standard))
        {
            formats.Insert(
                0,
                TextFormatCollection.Default.GetById(TextFormatId.Standard));
        }

        return new TextFormatCollection(formats);
    }

    private static DimensionStyleCollection FromDimensionStyleDtos(IReadOnlyCollection<DimensionStyleDto>? dtos)
    {
        if (dtos is null || dtos.Count == 0)
        {
            return DimensionStyleCollection.Default;
        }

        var styles = new List<DimensionStyle>();

        foreach (DimensionStyleDto dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(dto.Name)
                ? dto.Id
                : dto.Name;

            TextFormatId textFormatId = string.IsNullOrWhiteSpace(dto.TextFormatId)
                ? TextFormatId.Annotation
                : new TextFormatId(dto.TextFormatId);

            styles.Add(new DimensionStyle(
                new DimensionStyleId(dto.Id),
                name,
                textFormatId,
                dto.ArrowSize <= 0 ? 4.0 : dto.ArrowSize,
                dto.TextOffset < 0 ? 2.0 : dto.TextOffset,
                dto.ExtensionLineOffset < 0 ? 1.5 : dto.ExtensionLineOffset,
                dto.ExtensionLineOvershoot < 0 ? 2.0 : dto.ExtensionLineOvershoot,
                dto.DecimalPlaces < 0 ? 2 : Math.Min(dto.DecimalPlaces, 8),
                string.IsNullOrWhiteSpace(dto.DecimalSeparator) ? "." : dto.DecimalSeparator,
                dto.Suffix ?? string.Empty));
        }

        if (styles.Count == 0)
        {
            return DimensionStyleCollection.Default;
        }

        if (!styles.Any(style => style.Id == DimensionStyleId.Standard))
        {
            styles.Insert(
                0,
                DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard));
        }

        return new DimensionStyleCollection(styles);
    }

    private static Layer FromDto(
        LayerDto dto,
        LineFormatCollection lineFormats)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new DocumentLoadException(
                "A layer has an empty id.");
        }

        LineFormatId lineFormatId = string.IsNullOrWhiteSpace(dto.LineFormatId)
            ? LineFormatId.Continuous
            : new LineFormatId(dto.LineFormatId);

        if (!lineFormats.Contains(lineFormatId))
        {
            lineFormatId = LineFormatId.Continuous;
        }

        return new Layer(
            new LayerId(dto.Id),
            string.IsNullOrWhiteSpace(dto.Name) ? dto.Id : dto.Name,
            lineFormatId,
            dto.IsVisible,
            dto.IsLocked);
    }

    private static CadEntity? FromDto(EntityDto dto)
    {
        if (dto is UnknownEntityDto)
        {
            return null;
        }

        EntityId id = ParseEntityId(dto.Id);
        LayerId layerId = new(string.IsNullOrWhiteSpace(dto.LayerId) ? LayerId.Default.Value : dto.LayerId);

        return dto switch
        {
            PointEntityDto point => new PointEntity(
                new Point2D(point.X, point.Y),
                id,
                layerId),

            TextEntityDto text => string.IsNullOrWhiteSpace(text.Text)
                ? null
                : new TextEntity(
                    new Point2D(text.InsertionX, text.InsertionY),
                    text.Text,
                    text.RotationDegrees,
                    string.IsNullOrWhiteSpace(text.TextFormatId)
                        ? TextFormatId.Standard
                        : new TextFormatId(text.TextFormatId),
                    id,
                    layerId),

            MultilineTextEntityDto multilineText => string.IsNullOrWhiteSpace(multilineText.Text)
                ? null
                : new MultilineTextEntity(
                    new Point2D(multilineText.InsertionX, multilineText.InsertionY),
                    multilineText.Text,
                    multilineText.RotationDegrees,
                    string.IsNullOrWhiteSpace(multilineText.TextFormatId)
                        ? TextFormatId.Standard
                        : new TextFormatId(multilineText.TextFormatId),
                    id,
                    layerId),

            LinearDimensionEntityDto linearDimension => new LinearDimensionEntity(
                new Point2D(linearDimension.FirstX, linearDimension.FirstY),
                new Point2D(linearDimension.SecondX, linearDimension.SecondY),
                new Point2D(linearDimension.DimensionLineX, linearDimension.DimensionLineY),
                ParseDimensionOrientation(linearDimension.Orientation),
                string.IsNullOrWhiteSpace(linearDimension.DimensionStyleId)
                    ? DimensionStyleId.Standard
                    : new DimensionStyleId(linearDimension.DimensionStyleId),
                linearDimension.TextOverride,
                id,
                layerId,
                isStale: linearDimension.IsStale),

            AlignedDimensionEntityDto alignedDimension => new AlignedDimensionEntity(
                new Point2D(alignedDimension.FirstX, alignedDimension.FirstY),
                new Point2D(alignedDimension.SecondX, alignedDimension.SecondY),
                new Point2D(alignedDimension.DimensionLineX, alignedDimension.DimensionLineY),
                string.IsNullOrWhiteSpace(alignedDimension.DimensionStyleId)
                    ? DimensionStyleId.Standard
                    : new DimensionStyleId(alignedDimension.DimensionStyleId),
                alignedDimension.TextOverride,
                id,
                layerId,
                isStale: alignedDimension.IsStale),

            RadiusDimensionEntityDto radiusDimension => new RadiusDimensionEntity(
                new Point2D(radiusDimension.CenterX, radiusDimension.CenterY),
                new Point2D(radiusDimension.PointOnCircleX, radiusDimension.PointOnCircleY),
                new Point2D(radiusDimension.TextX, radiusDimension.TextY),
                string.IsNullOrWhiteSpace(radiusDimension.DimensionStyleId)
                    ? DimensionStyleId.Standard
                    : new DimensionStyleId(radiusDimension.DimensionStyleId),
                radiusDimension.TextOverride,
                id,
                layerId,
                isStale: radiusDimension.IsStale),

            DiameterDimensionEntityDto diameterDimension => new DiameterDimensionEntity(
                new Point2D(diameterDimension.CenterX, diameterDimension.CenterY),
                new Point2D(diameterDimension.PointOnCircleX, diameterDimension.PointOnCircleY),
                new Point2D(diameterDimension.TextX, diameterDimension.TextY),
                string.IsNullOrWhiteSpace(diameterDimension.DimensionStyleId)
                    ? DimensionStyleId.Standard
                    : new DimensionStyleId(diameterDimension.DimensionStyleId),
                diameterDimension.TextOverride,
                id,
                layerId,
                isStale: diameterDimension.IsStale),

            AngularDimensionEntityDto angularDimension => new AngularDimensionEntity(
                new Point2D(angularDimension.CenterX, angularDimension.CenterY),
                new Point2D(angularDimension.FirstRayX, angularDimension.FirstRayY),
                new Point2D(angularDimension.SecondRayX, angularDimension.SecondRayY),
                new Point2D(angularDimension.ArcX, angularDimension.ArcY),
                angularDimension.IsCounterClockwise,
                string.IsNullOrWhiteSpace(angularDimension.DimensionStyleId)
                    ? DimensionStyleId.Standard
                    : new DimensionStyleId(angularDimension.DimensionStyleId),
                angularDimension.TextOverride,
                id,
                layerId,
                isStale: angularDimension.IsStale),

            LineEntityDto line => new LineEntity(
                new Point2D(line.StartX, line.StartY),
                new Point2D(line.EndX, line.EndY),
                id,
                layerId),

            CircleEntityDto circle => new CircleEntity(
                new Point2D(circle.CenterX, circle.CenterY),
                circle.Radius,
                id,
                layerId),

            EllipseEntityDto ellipse => ellipse.MinorRadius <= 0 || new Vector2D(ellipse.MajorAxisX, ellipse.MajorAxisY).Length <= 0
                ? null
                : new EllipseEntity(
                    new Point2D(ellipse.CenterX, ellipse.CenterY),
                    new Vector2D(ellipse.MajorAxisX, ellipse.MajorAxisY),
                    ellipse.MinorRadius,
                    id,
                    layerId),

            ArcEntityDto arc => new ArcEntity(
                new Point2D(arc.CenterX, arc.CenterY),
                arc.Radius,
                Angle.FromDegrees(arc.StartAngleDegrees),
                Angle.FromDegrees(arc.EndAngleDegrees),
                arc.IsCounterClockwise,
                id,
                layerId),

            PolylineEntityDto polyline => new PolylineEntity(
                polyline.Vertices.Select(vertex => new Point2D(vertex.X, vertex.Y)),
                polyline.IsClosed,
                id,
                layerId),

            BezierSplineEntityDto spline => spline.ControlPoints.Count < 2
                ? null
                : new BezierSplineEntity(
                    spline.ControlPoints.Select(point => new Point2D(point.X, point.Y)),
                    spline.IsClosed,
                    id,
                    layerId),

            _ => null
        };
    }

    private static DimensionOrientation ParseDimensionOrientation(string? value)
    {
        return Enum.TryParse(
            value,
            ignoreCase: true,
            out DimensionOrientation result)
            ? result
            : DimensionOrientation.Horizontal;
    }

    private static LineStyle ParseLineStyle(string? value)
    {
        return Enum.TryParse(
            value,
            ignoreCase: true,
            out LineStyle result)
            ? result
            : LineStyle.Continuous;
    }

    private static EntityId ParseEntityId(string value)
    {
        if (!Guid.TryParse(value, out Guid guid))
        {
            throw new DocumentLoadException(
                $"Invalid entity id '{value}'.");
        }

        return new EntityId(guid);
    }

    private static string ToHex(CadColor color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static CadColor FromHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CadColor.FromRgb(255, 255, 255);
        }

        string hex = value.Trim();

        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length != 6)
        {
            return CadColor.FromRgb(255, 255, 255);
        }

        try
        {
            byte r = Convert.ToByte(hex[0..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);

            return CadColor.FromRgb(r, g, b);
        }
        catch (FormatException)
        {
            return CadColor.FromRgb(255, 255, 255);
        }
    }

    private static DocumentSettingsDto NormalizeSettings(
        DocumentSettingsDto? settings,
        string currentLayerId)
    {
        DocumentSettingsDto normalized = settings ?? new DocumentSettingsDto
        {
            CurrentLayerId = currentLayerId
        };

        if (string.IsNullOrWhiteSpace(normalized.CurrentLayerId))
        {
            normalized.CurrentLayerId = currentLayerId;
        }

        normalized.Grid ??= new DocumentGridSettingsDto();
        normalized.Snapping ??= new DocumentSnapSettingsDto();
        normalized.Drafting ??= new DocumentDraftingSettingsDto();
        normalized.Drafting.PolarTracking ??= new DocumentPolarTrackingSettingsDto();

        if (string.IsNullOrWhiteSpace(normalized.CurrentTextFormatId))
        {
            normalized.CurrentTextFormatId = TextFormatId.Standard.Value;
        }

        return normalized;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        options.Converters.Add(new EntityDtoJsonConverter());

        return options;
    }
}
