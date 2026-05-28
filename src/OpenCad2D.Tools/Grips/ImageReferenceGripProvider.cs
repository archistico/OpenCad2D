using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides resize and move grips for external image references.
/// </summary>
public sealed class ImageReferenceGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is ImageReferenceEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        ImageReferenceEntity image = GetImage(entity);

        return new[]
        {
            new GripPoint(image.BottomLeft, GripKind.MoveVertex, image.Id, 0),
            new GripPoint(image.BottomRight, GripKind.MoveVertex, image.Id, 1),
            new GripPoint(image.TopRight, GripKind.MoveVertex, image.Id, 2),
            new GripPoint(image.TopLeft, GripKind.MoveVertex, image.Id, 3),
            new GripPoint(image.Center, GripKind.MoveEntity, image.Id, 4)
        };
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        ImageReferenceEntity image = GetImage(entity);

        return gripIndex switch
        {
            0 => RebuildFromCorners(
                image,
                destination,
                image.BottomRight,
                image.TopLeft),

            1 => RebuildFromCorners(
                image,
                image.BottomLeft,
                destination,
                image.TopLeft),

            2 => RebuildFromCorners(
                image,
                image.BottomLeft,
                image.BottomRight,
                destination - image.WidthVector),

            3 => RebuildFromCorners(
                image,
                image.BottomLeft,
                image.BottomRight,
                destination),

            4 => MoveWholeImage(
                image,
                destination),

            _ => throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown image reference grip index.")
        };
    }

    private static ImageReferenceEntity MoveWholeImage(
        ImageReferenceEntity image,
        Point2D destination)
    {
        Vector2D vector = image.Center.VectorTo(destination);

        return new ImageReferenceEntity(
            image.FilePath,
            image.Origin + vector,
            image.WidthVector,
            image.HeightVector,
            image.PixelWidth,
            image.PixelHeight,
            image.Id,
            image.LayerId,
            image.Style,
            image.IsVisible,
            image.IsLocked,
            image.DrawOrder,
            image.Opacity);
    }

    private static ImageReferenceEntity RebuildFromCorners(
        ImageReferenceEntity image,
        Point2D bottomLeft,
        Point2D bottomRight,
        Point2D topLeftOrTopRight)
    {
        Vector2D widthVector = bottomLeft.VectorTo(bottomRight);
        Vector2D heightVector = bottomLeft.VectorTo(topLeftOrTopRight);

        if (widthVector.Length <= 1e-9 || heightVector.Length <= 1e-9)
        {
            return image;
        }

        return new ImageReferenceEntity(
            image.FilePath,
            bottomLeft,
            widthVector,
            heightVector,
            image.PixelWidth,
            image.PixelHeight,
            image.Id,
            image.LayerId,
            image.Style,
            image.IsVisible,
            image.IsLocked,
            image.DrawOrder,
            image.Opacity);
    }

    private static ImageReferenceEntity GetImage(CadEntity entity)
    {
        return entity as ImageReferenceEntity
            ?? throw new ArgumentException(
                "Entity must be an image reference entity.",
                nameof(entity));
    }
}
