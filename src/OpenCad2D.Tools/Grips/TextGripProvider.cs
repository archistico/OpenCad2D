using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides the insertion-point grip for single-line text entities.
/// </summary>
public sealed class TextGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is TextEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        TextEntity text = GetText(entity);

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
        TextEntity text = GetText(entity);

        if (gripIndex != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown text grip index.");
        }

        return new TextEntity(
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

    private static TextEntity GetText(CadEntity entity)
    {
        return entity as TextEntity
            ?? throw new ArgumentException(
                "Entity must be a text entity.",
                nameof(entity));
    }
}
