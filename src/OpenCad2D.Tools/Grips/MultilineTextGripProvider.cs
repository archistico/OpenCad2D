using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides the insertion-point grip for multiline text entities.
/// </summary>
public sealed class MultilineTextGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is MultilineTextEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        MultilineTextEntity text = GetText(entity);

        return new[]
        {
            new GripPoint(
                text.InsertionPoint,
                GripKind.MoveEntity,
                text.Id,
                0)
        };
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        MultilineTextEntity text = GetText(entity);

        if (gripIndex != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown multiline text grip index.");
        }

        return new MultilineTextEntity(
            destination,
            text.Text,
            text.RotationDegrees,
            text.TextFormatId,
            text.Id,
            text.LayerId,
            text.Style,
            text.IsVisible,
            text.IsLocked,
            text.DrawOrder);
    }

    private static MultilineTextEntity GetText(CadEntity entity)
    {
        return entity as MultilineTextEntity
            ?? throw new ArgumentException(
                "Entity must be a multiline text entity.",
                nameof(entity));
    }
}
