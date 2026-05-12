using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class DimensionRoundTripTests
{
    [Fact]
    public void SerializeDeserialize_ShouldPreserveDimensionStylesAndLinearDimensions()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();
        var customStyleId = new DimensionStyleId("Architectural");

        document.ReplaceDimensionStyles(new DimensionStyleCollection(new[]
        {
            DimensionStyleCollection.Default.GetById(DimensionStyleId.Standard),
            new DimensionStyle(
                customStyleId,
                "Architectural",
                TextFormatId.Annotation,
                arrowSize: 5,
                textOffset: 3,
                extensionLineOffset: 1,
                extensionLineOvershoot: 2,
                decimalPlaces: 1,
                decimalSeparator: ",",
                suffix: " mm")
        }));

        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal,
            customStyleId,
            textOverride: "100 typ.");

        document.AddEntity(dimension);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        Assert.True(restored.DimensionStyles.Contains(customStyleId));
        DimensionStyle restoredStyle = restored.DimensionStyles.GetById(customStyleId);
        Assert.Equal(5, restoredStyle.ArrowSize);
        Assert.Equal(1, restoredStyle.DecimalPlaces);
        Assert.Equal(",", restoredStyle.DecimalSeparator);
        Assert.Equal(" mm", restoredStyle.Suffix);

        var restoredDimension = Assert.IsType<LinearDimensionEntity>(restored.Entities.GetRequired(dimension.Id));
        Assert.Equal(dimension.FirstPoint, restoredDimension.FirstPoint);
        Assert.Equal(dimension.SecondPoint, restoredDimension.SecondPoint);
        Assert.Equal(dimension.DimensionLinePoint, restoredDimension.DimensionLinePoint);
        Assert.Equal(DimensionOrientation.Horizontal, restoredDimension.Orientation);
        Assert.Equal(customStyleId, restoredDimension.DimensionStyleId);
        Assert.Equal("100 typ.", restoredDimension.TextOverride);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveAlignedDimensions()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        var dimension = new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(0, 6));

        document.AddEntity(dimension);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        var restoredDimension = Assert.IsType<AlignedDimensionEntity>(restored.Entities.GetRequired(dimension.Id));
        Assert.Equal(dimension.FirstPoint, restoredDimension.FirstPoint);
        Assert.Equal(dimension.SecondPoint, restoredDimension.SecondPoint);
        Assert.Equal(dimension.DimensionLinePoint, restoredDimension.DimensionLinePoint);
        Assert.Equal(5, restoredDimension.MeasurementValue, precision: 10);
    }

    [Fact]
    public void SerializeDeserialize_ShouldPreserveRadiusAndDiameterDimensions()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        var radius = new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2));
        var diameter = new DiameterDimensionEntity(
            new Point2D(20, 0),
            new Point2D(25, 0),
            new Point2D(30, 2));

        document.AddEntity(radius);
        document.AddEntity(diameter);

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out _,
            out _);

        var restoredRadius = Assert.IsType<RadiusDimensionEntity>(restored.Entities.GetRequired(radius.Id));
        Assert.Equal(radius.Center, restoredRadius.Center);
        Assert.Equal(radius.PointOnCircle, restoredRadius.PointOnCircle);
        Assert.Equal(radius.TextPoint, restoredRadius.TextPoint);
        Assert.Equal(10, restoredRadius.MeasurementValue);

        var restoredDiameter = Assert.IsType<DiameterDimensionEntity>(restored.Entities.GetRequired(diameter.Id));
        Assert.Equal(diameter.Center, restoredDiameter.Center);
        Assert.Equal(diameter.PointOnCircle, restoredDiameter.PointOnCircle);
        Assert.Equal(diameter.TextPoint, restoredDiameter.TextPoint);
        Assert.Equal(10, restoredDiameter.MeasurementValue);
    }

    [Fact]
    public void JsonRoundTrip_ShouldPreserveDimensionEntityTypes()
    {
        var serializer = new JsonDocumentSerializer();
        var document = new CadDocument();

        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 5),
            DimensionOrientation.Horizontal));

        document.AddEntity(new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(0, 6)));

        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        string json = JsonDocumentSerializer.ToJson(dto);
        DocumentDto loadedDto = JsonDocumentSerializer.FromJson(json);

        Assert.Contains(loadedDto.Entities, entity => entity is LinearDimensionEntityDto);
        Assert.Contains(loadedDto.Entities, entity => entity is AlignedDimensionEntityDto);
        Assert.NotEmpty(loadedDto.DimensionStyles);
    }
}
