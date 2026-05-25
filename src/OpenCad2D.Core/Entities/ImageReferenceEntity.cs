using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing an externally linked raster image.
/// </summary>
/// <remarks>
/// The image file is referenced by path and is never embedded in the drawing.
/// Geometry is stored as an oriented rectangle using an origin plus width and height vectors.
/// </remarks>
public sealed class ImageReferenceEntity : CadEntity
{
    public ImageReferenceEntity(
        string filePath,
        Point2D origin,
        Vector2D widthVector,
        Vector2D heightVector,
        int pixelWidth = 0,
        int pixelHeight = 0,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Image file path cannot be empty.",
                nameof(filePath));
        }

        if (widthVector.Length <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(widthVector),
                "Image width vector must not be zero.");
        }

        if (heightVector.Length <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heightVector),
                "Image height vector must not be zero.");
        }

        FilePath = filePath;
        Origin = origin;
        WidthVector = widthVector;
        HeightVector = heightVector;
        PixelWidth = Math.Max(0, pixelWidth);
        PixelHeight = Math.Max(0, pixelHeight);
    }

    public string FilePath { get; }

    public Point2D Origin { get; }

    public Vector2D WidthVector { get; }

    public Vector2D HeightVector { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public Point2D BottomLeft => Origin;

    public Point2D BottomRight => Origin + WidthVector;

    public Point2D TopLeft => Origin + HeightVector;

    public Point2D TopRight => Origin + WidthVector + HeightVector;

    public Point2D Center => Origin + ((WidthVector + HeightVector) / 2.0);

    public double Width => WidthVector.Length;

    public double Height => HeightVector.Length;

    public double RotationDegrees => Math.Atan2(WidthVector.Y, WidthVector.X) * 180.0 / Math.PI;

    public double NaturalAspectRatio => PixelWidth > 0 && PixelHeight > 0
        ? PixelHeight / (double)PixelWidth
        : 0.0;

    public bool HasNaturalAspectRatio => NaturalAspectRatio > 0;

    public ImageReferenceEntity WithFilePath(
        string filePath,
        int pixelWidth,
        int pixelHeight)
    {
        return new ImageReferenceEntity(
            filePath,
            Origin,
            WidthVector,
            HeightVector,
            pixelWidth,
            pixelHeight,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public ImageReferenceEntity WithOrigin(Point2D origin)
    {
        return new ImageReferenceEntity(
            FilePath,
            origin,
            WidthVector,
            HeightVector,
            PixelWidth,
            PixelHeight,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public ImageReferenceEntity WithSize(
        double width,
        double height)
    {
        return WithSizeInternal(
            width,
            height,
            keepCenter: false);
    }

    public ImageReferenceEntity WithSizeAroundCenter(
        double width,
        double height)
    {
        return WithSizeInternal(
            width,
            height,
            keepCenter: true);
    }

    public ImageReferenceEntity WithNaturalAspectRatio()
    {
        if (!HasNaturalAspectRatio)
        {
            return this;
        }

        return WithSizeAroundCenter(
            Width,
            Math.Max(1e-9, Width * NaturalAspectRatio));
    }

    private ImageReferenceEntity WithSizeInternal(
        double width,
        double height,
        bool keepCenter)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Image width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                "Image height must be greater than zero.");
        }

        Vector2D widthDirection = WidthVector.Normalize();
        Vector2D heightDirection = HeightVector.Normalize();

        Vector2D widthVector = widthDirection * width;
        Vector2D heightVector = heightDirection * height;
        Point2D origin = keepCenter
            ? Center - ((widthVector + heightVector) / 2.0)
            : Origin;

        return new ImageReferenceEntity(
            FilePath,
            origin,
            widthVector,
            heightVector,
            PixelWidth,
            PixelHeight,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public ImageReferenceEntity WithRotationDegrees(double rotationDegrees)
    {
        double radians = rotationDegrees * Math.PI / 180.0;
        Vector2D widthDirection = new(Math.Cos(radians), Math.Sin(radians));
        double orientationSign = WidthVector.Cross(HeightVector) < 0 ? -1.0 : 1.0;
        Vector2D heightDirection = orientationSign > 0
            ? widthDirection.PerpendicularLeft()
            : widthDirection.PerpendicularRight();
        Point2D center = Center;
        Vector2D widthVector = widthDirection * Width;
        Vector2D heightVector = heightDirection * Height;
        Point2D origin = center - ((widthVector + heightVector) / 2.0);

        return new ImageReferenceEntity(
            FilePath,
            origin,
            widthVector,
            heightVector,
            PixelWidth,
            PixelHeight,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override EntityKind Kind => EntityKind.ImageReference;

    public IReadOnlyList<Point2D> GetCorners()
    {
        return new[]
        {
            BottomLeft,
            BottomRight,
            TopRight,
            TopLeft
        };
    }

    public override BoundingBox2D GetBoundingBox()
    {
        IReadOnlyList<Point2D> corners = GetCorners();

        return new BoundingBox2D(
            corners.Min(point => point.X),
            corners.Min(point => point.Y),
            corners.Max(point => point.X),
            corners.Max(point => point.Y));
    }

    public override double DistanceTo(Point2D point)
    {
        if (Contains(point))
        {
            return 0;
        }

        return GetEdges()
            .Min(edge => DistanceService.DistancePointToSegment(point, edge));
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        if (Contains(point))
        {
            return point;
        }

        return GetEdges()
            .Select(edge => DistanceService.ClosestPointOnSegment(point, edge))
            .OrderBy(candidate => candidate.DistanceTo(point))
            .First();
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new ImageReferenceEntity(
            FilePath,
            matrix.Transform(Origin),
            matrix.Transform(WidthVector),
            matrix.Transform(HeightVector),
            PixelWidth,
            PixelHeight,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new ImageReferenceEntity(
            FilePath,
            Origin,
            WidthVector,
            HeightVector,
            PixelWidth,
            PixelHeight,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new ImageReferenceEntity(
            FilePath,
            Origin,
            WidthVector,
            HeightVector,
            PixelWidth,
            PixelHeight,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    private bool Contains(Point2D point)
    {
        Vector2D relative = Origin.VectorTo(point);
        double determinant = WidthVector.Cross(HeightVector);

        if (Math.Abs(determinant) <= 1e-12)
        {
            return false;
        }

        double u = relative.Cross(HeightVector) / determinant;
        double v = WidthVector.Cross(relative) / determinant;

        return u >= 0 && u <= 1 && v >= 0 && v <= 1;
    }

    private IReadOnlyList<LineSegment2D> GetEdges()
    {
        return new[]
        {
            new LineSegment2D(BottomLeft, BottomRight),
            new LineSegment2D(BottomRight, TopRight),
            new LineSegment2D(TopRight, TopLeft),
            new LineSegment2D(TopLeft, BottomLeft)
        };
    }
}
