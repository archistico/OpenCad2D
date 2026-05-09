using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides grips for line entities.
/// </summary>
public sealed class LineGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is LineEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        LineEntity line = GetLine(entity);
        Point2D midpoint = GetMidpoint(line);

        return new[]
        {
            new GripPoint(
                line.Start,
                GripKind.MoveVertex,
                line.Id,
                0),

            new GripPoint(
                midpoint,
                GripKind.MoveEntity,
                line.Id,
                1),

            new GripPoint(
                line.End,
                GripKind.MoveVertex,
                line.Id,
                2)
        };
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        LineEntity line = GetLine(entity);

        return gripIndex switch
        {
            0 => new LineEntity(
                destination,
                line.End,
                line.Id,
                line.LayerId,
                line.Style,
                line.IsVisible,
                line.IsLocked,
                line.DrawOrder),

            1 => MoveWholeLine(
                line,
                destination),

            2 => new LineEntity(
                line.Start,
                destination,
                line.Id,
                line.LayerId,
                line.Style,
                line.IsVisible,
                line.IsLocked,
                line.DrawOrder),

            _ => throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown line grip index.")
        };
    }

    private static LineEntity MoveWholeLine(
        LineEntity line,
        Point2D destination)
    {
        Point2D midpoint = GetMidpoint(line);
        Vector2D vector = midpoint.VectorTo(destination);

        return new LineEntity(
            line.Start + vector,
            line.End + vector,
            line.Id,
            line.LayerId,
            line.Style,
            line.IsVisible,
            line.IsLocked,
            line.DrawOrder);
    }

    private static Point2D GetMidpoint(LineEntity line)
    {
        return new Point2D(
            (line.Start.X + line.End.X) / 2.0,
            (line.Start.Y + line.End.Y) / 2.0);
    }

    private static LineEntity GetLine(CadEntity entity)
    {
        return entity as LineEntity
            ?? throw new ArgumentException(
                "Entity must be a line entity.",
                nameof(entity));
    }
}
