using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing.Curves;

/// <summary>
/// Shared native curve splitting pipeline used by TRIM/BREAK-style operations.
/// </summary>
public sealed class CadCurveSplitService
{
    private readonly ICurveAdapterFactory _adapterFactory;

    public CadCurveSplitService()
        : this(new DefaultCurveAdapterFactory())
    {
    }

    public CadCurveSplitService(ICurveAdapterFactory adapterFactory)
    {
        _adapterFactory = adapterFactory;
    }

    public IReadOnlyList<CadEntity> SplitAtPoint(
        CadEntity entity,
        Point2D point,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (!TryCreateAdapterAndCut(
                entity,
                point,
                effectiveTolerance,
                out ICurveAdapter adapter,
                out CurveCut cut))
        {
            return Array.Empty<CadEntity>();
        }

        if (adapter.IsClosed)
        {
            return adapter.BuildFragments(
                new[]
                {
                    new CurveInterval(
                        cut,
                        new CurveCut(cut.Parameter + adapter.Period, cut.Point))
                },
                effectiveTolerance);
        }

        if (cut.Parameter <= adapter.StartParameter + effectiveTolerance.Parameter ||
            cut.Parameter >= adapter.EndParameter - effectiveTolerance.Parameter)
        {
            return Array.Empty<CadEntity>();
        }

        return adapter.BuildFragments(
            new[]
            {
                new CurveInterval(
                    new CurveCut(adapter.StartParameter, adapter.PointAt(adapter.StartParameter)),
                    cut),
                new CurveInterval(
                    cut,
                    new CurveCut(adapter.EndParameter, adapter.PointAt(adapter.EndParameter)))
            },
            effectiveTolerance);
    }

    public IReadOnlyList<CadEntity> RemoveBetweenPoints(
        CadEntity entity,
        Point2D firstPoint,
        Point2D secondPoint,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (!_adapterFactory.TryCreate(entity, out ICurveAdapter adapter) ||
            !adapter.TryProjectPointToCut(firstPoint, effectiveTolerance, out CurveCut firstCut) ||
            !adapter.TryProjectPointToCut(secondPoint, effectiveTolerance, out CurveCut secondCut))
        {
            return Array.Empty<CadEntity>();
        }

        return RemoveBetweenCuts(
            adapter,
            firstCut,
            secondCut,
            effectiveTolerance);
    }

    public IReadOnlyList<CadEntity> RemovePickedInterval(
        CadEntity entity,
        IReadOnlyList<Point2D> cutPoints,
        Point2D pickPoint,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (!_adapterFactory.TryCreate(entity, out ICurveAdapter adapter))
        {
            return Array.Empty<CadEntity>();
        }

        var cuts = new List<CurveCut>();
        foreach (Point2D point in cutPoints)
        {
            if (adapter.TryProjectPointToCut(point, effectiveTolerance, out CurveCut cut))
            {
                cuts.Add(cut);
            }
        }

        return RemovePickedInterval(
            adapter,
            cuts,
            pickPoint,
            effectiveTolerance);
    }

    public IReadOnlyList<CadEntity> RemovePickedInterval(
        CadEntity entity,
        IReadOnlyList<CurveCut> cuts,
        Point2D pickPoint,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (!_adapterFactory.TryCreate(entity, out ICurveAdapter adapter))
        {
            return Array.Empty<CadEntity>();
        }

        return RemovePickedInterval(
            adapter,
            cuts,
            pickPoint,
            effectiveTolerance);
    }


    public IReadOnlyList<CadEntity> GetPickedInterval(
        CadEntity entity,
        IReadOnlyList<Point2D> cutPoints,
        Point2D pickPoint,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (!_adapterFactory.TryCreate(entity, out ICurveAdapter adapter))
        {
            return Array.Empty<CadEntity>();
        }

        var cuts = new List<CurveCut>();
        foreach (Point2D point in cutPoints)
        {
            if (adapter.TryProjectPointToCut(point, effectiveTolerance, out CurveCut cut))
            {
                cuts.Add(cut);
            }
        }

        return GetPickedInterval(
            adapter,
            cuts,
            pickPoint,
            effectiveTolerance);
    }

    public IReadOnlyList<CadEntity> GetPickedInterval(
        CadEntity entity,
        IReadOnlyList<CurveCut> cuts,
        Point2D pickPoint,
        GeometryTolerance? tolerance = null)
    {
        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (!_adapterFactory.TryCreate(entity, out ICurveAdapter adapter))
        {
            return Array.Empty<CadEntity>();
        }

        return GetPickedInterval(
            adapter,
            cuts,
            pickPoint,
            effectiveTolerance);
    }

    private IReadOnlyList<CadEntity> RemovePickedInterval(
        ICurveAdapter adapter,
        IReadOnlyList<CurveCut> cuts,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        if (!adapter.TryProjectPointToCut(pickPoint, tolerance, out CurveCut pickCut))
        {
            return Array.Empty<CadEntity>();
        }

        List<CurveCut> normalizedCuts = NormalizeCuts(
            cuts,
            adapter,
            tolerance);

        if (adapter.IsClosed)
        {
            if (normalizedCuts.Count < 2)
            {
                return Array.Empty<CadEntity>();
            }

            return RemovePickedClosedInterval(
                adapter,
                normalizedCuts,
                pickCut,
                tolerance);
        }

        if (normalizedCuts.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        return RemovePickedOpenInterval(
            adapter,
            normalizedCuts,
            pickCut,
            tolerance);
    }

    private IReadOnlyList<CadEntity> RemovePickedOpenInterval(
        ICurveAdapter adapter,
        IReadOnlyList<CurveCut> cuts,
        CurveCut pickCut,
        GeometryTolerance tolerance)
    {
        var pathCuts = new List<CurveCut>
        {
            new(adapter.StartParameter, adapter.PointAt(adapter.StartParameter))
        };
        pathCuts.AddRange(cuts.Where(cut =>
            cut.Parameter > adapter.StartParameter + tolerance.Parameter &&
            cut.Parameter < adapter.EndParameter - tolerance.Parameter));
        pathCuts.Add(new CurveCut(adapter.EndParameter, adapter.PointAt(adapter.EndParameter)));

        pathCuts = NormalizeCuts(
            pathCuts,
            adapter,
            tolerance);

        if (pathCuts.Count <= 2)
        {
            return Array.Empty<CadEntity>();
        }

        int intervalToRemove = FindOpenIntervalContaining(
            pathCuts,
            pickCut.Parameter,
            tolerance);
        var intervalsToKeep = new List<CurveInterval>();

        for (int index = 0; index < pathCuts.Count - 1; index++)
        {
            if (index == intervalToRemove)
            {
                continue;
            }

            if (pathCuts[index + 1].Parameter - pathCuts[index].Parameter <= tolerance.Parameter)
            {
                continue;
            }

            intervalsToKeep.Add(new CurveInterval(pathCuts[index], pathCuts[index + 1]));
        }

        return adapter.BuildFragments(
            MergeContiguousOpenIntervals(intervalsToKeep, tolerance),
            tolerance);
    }


    private IReadOnlyList<CadEntity> GetPickedInterval(
        ICurveAdapter adapter,
        IReadOnlyList<CurveCut> cuts,
        Point2D pickPoint,
        GeometryTolerance tolerance)
    {
        if (!adapter.TryProjectPointToCut(pickPoint, tolerance, out CurveCut pickCut))
        {
            return Array.Empty<CadEntity>();
        }

        List<CurveCut> normalizedCuts = NormalizeCuts(
            cuts,
            adapter,
            tolerance);

        if (adapter.IsClosed)
        {
            if (normalizedCuts.Count < 2)
            {
                return Array.Empty<CadEntity>();
            }

            return BuildPickedClosedInterval(
                adapter,
                normalizedCuts,
                pickCut,
                tolerance);
        }

        if (normalizedCuts.Count == 0)
        {
            return Array.Empty<CadEntity>();
        }

        return BuildPickedOpenInterval(
            adapter,
            normalizedCuts,
            pickCut,
            tolerance);
    }

    private IReadOnlyList<CadEntity> BuildPickedOpenInterval(
        ICurveAdapter adapter,
        IReadOnlyList<CurveCut> cuts,
        CurveCut pickCut,
        GeometryTolerance tolerance)
    {
        var pathCuts = new List<CurveCut>
        {
            new(adapter.StartParameter, adapter.PointAt(adapter.StartParameter))
        };
        pathCuts.AddRange(cuts.Where(cut =>
            cut.Parameter > adapter.StartParameter + tolerance.Parameter &&
            cut.Parameter < adapter.EndParameter - tolerance.Parameter));
        pathCuts.Add(new CurveCut(adapter.EndParameter, adapter.PointAt(adapter.EndParameter)));

        pathCuts = NormalizeCuts(
            pathCuts,
            adapter,
            tolerance);

        if (pathCuts.Count <= 2)
        {
            return Array.Empty<CadEntity>();
        }

        int intervalToRemove = FindOpenIntervalContaining(
            pathCuts,
            pickCut.Parameter,
            tolerance);

        CurveCut start = pathCuts[intervalToRemove];
        CurveCut end = pathCuts[intervalToRemove + 1];

        if (end.Parameter - start.Parameter <= tolerance.Parameter)
        {
            return Array.Empty<CadEntity>();
        }

        return adapter.BuildFragments(
            new[]
            {
                new CurveInterval(start, end)
            },
            tolerance);
    }

    private IReadOnlyList<CadEntity> BuildPickedClosedInterval(
        ICurveAdapter adapter,
        IReadOnlyList<CurveCut> cuts,
        CurveCut pickCut,
        GeometryTolerance tolerance)
    {
        for (int index = 0; index < cuts.Count; index++)
        {
            CurveCut start = cuts[index];
            CurveCut end = cuts[(index + 1) % cuts.Count];
            double endParameter = end.Parameter;

            if (endParameter <= start.Parameter)
            {
                endParameter += adapter.Period;
            }

            double pickParameter = pickCut.Parameter;
            if (pickParameter < start.Parameter)
            {
                pickParameter += adapter.Period;
            }

            bool containsPick = ClosedIntervalContainsPick(
                start.Parameter,
                endParameter,
                pickParameter,
                tolerance);

            if (!containsPick ||
                endParameter - start.Parameter <= tolerance.Parameter)
            {
                continue;
            }

            return adapter.BuildFragments(
                new[]
                {
                    new CurveInterval(
                        start,
                        new CurveCut(endParameter, end.Point))
                },
                tolerance);
        }

        return Array.Empty<CadEntity>();
    }

    private static IReadOnlyList<CurveInterval> MergeContiguousOpenIntervals(
        IReadOnlyList<CurveInterval> intervals,
        GeometryTolerance tolerance)
    {
        if (intervals.Count <= 1)
        {
            return intervals;
        }

        var merged = new List<CurveInterval>();

        foreach (CurveInterval interval in intervals)
        {
            if (merged.Count > 0 &&
                Math.Abs(merged[^1].End.Parameter - interval.Start.Parameter) <= tolerance.Parameter &&
                merged[^1].End.Point.DistanceTo(interval.Start.Point) <= tolerance.Distance)
            {
                CurveInterval previous = merged[^1];
                merged[^1] = new CurveInterval(previous.Start, interval.End);
                continue;
            }

            merged.Add(interval);
        }

        return merged;
    }

    private IReadOnlyList<CadEntity> RemovePickedClosedInterval(
        ICurveAdapter adapter,
        IReadOnlyList<CurveCut> cuts,
        CurveCut pickCut,
        GeometryTolerance tolerance)
    {
        var intervalsToKeep = new List<CurveInterval>();

        for (int index = 0; index < cuts.Count; index++)
        {
            CurveCut start = cuts[index];
            CurveCut end = cuts[(index + 1) % cuts.Count];
            double endParameter = end.Parameter;

            if (endParameter <= start.Parameter)
            {
                endParameter += adapter.Period;
            }

            double pickParameter = pickCut.Parameter;
            if (pickParameter < start.Parameter)
            {
                pickParameter += adapter.Period;
            }

            bool containsPick = ClosedIntervalContainsPick(
                start.Parameter,
                endParameter,
                pickParameter,
                tolerance);

            if (containsPick ||
                endParameter - start.Parameter <= tolerance.Parameter)
            {
                continue;
            }

            intervalsToKeep.Add(new CurveInterval(
                start,
                new CurveCut(endParameter, end.Point)));
        }

        return adapter.BuildFragments(
            intervalsToKeep,
            tolerance);
    }

    private static bool ClosedIntervalContainsPick(
        double startParameter,
        double endParameter,
        double pickParameter,
        GeometryTolerance tolerance)
    {
        if (Math.Abs(pickParameter - startParameter) <= tolerance.Parameter)
        {
            return true;
        }

        if (Math.Abs(pickParameter - endParameter) <= tolerance.Parameter)
        {
            return false;
        }

        return pickParameter > startParameter &&
               pickParameter < endParameter;
    }

    private IReadOnlyList<CadEntity> RemoveBetweenCuts(
        ICurveAdapter adapter,
        CurveCut firstCut,
        CurveCut secondCut,
        GeometryTolerance tolerance)
    {
        if (firstCut.Point.DistanceTo(secondCut.Point) <= tolerance.Distance)
        {
            return Array.Empty<CadEntity>();
        }

        if (adapter.IsClosed)
        {
            double startParameter = firstCut.Parameter;
            double endParameter = secondCut.Parameter;

            if (endParameter <= startParameter)
            {
                endParameter += adapter.Period;
            }

            return adapter.BuildFragments(
                new[]
                {
                    new CurveInterval(
                        new CurveCut(endParameter, secondCut.Point),
                        new CurveCut(startParameter + adapter.Period, firstCut.Point))
                },
                tolerance);
        }

        CurveCut start = firstCut;
        CurveCut end = secondCut;

        if (start.Parameter > end.Parameter)
        {
            (start, end) = (end, start);
        }

        if (end.Parameter - start.Parameter <= tolerance.Parameter)
        {
            return Array.Empty<CadEntity>();
        }

        var intervalsToKeep = new List<CurveInterval>();
        CurveCut pathStart = new(adapter.StartParameter, adapter.PointAt(adapter.StartParameter));
        CurveCut pathEnd = new(adapter.EndParameter, adapter.PointAt(adapter.EndParameter));

        if (start.Parameter > adapter.StartParameter + tolerance.Parameter)
        {
            intervalsToKeep.Add(new CurveInterval(pathStart, start));
        }

        if (end.Parameter < adapter.EndParameter - tolerance.Parameter)
        {
            intervalsToKeep.Add(new CurveInterval(end, pathEnd));
        }

        return adapter.BuildFragments(
            intervalsToKeep,
            tolerance);
    }

    private bool TryCreateAdapterAndCut(
        CadEntity entity,
        Point2D point,
        GeometryTolerance tolerance,
        out ICurveAdapter adapter,
        out CurveCut cut)
    {
        if (!_adapterFactory.TryCreate(entity, out adapter!) ||
            !adapter.TryProjectPointToCut(point, tolerance, out cut))
        {
            adapter = null!;
            cut = default;
            return false;
        }

        return true;
    }

    private static List<CurveCut> NormalizeCuts(
        IEnumerable<CurveCut> cuts,
        ICurveAdapter adapter,
        GeometryTolerance tolerance)
    {
        var sortedCuts = cuts
            .Select(cut => adapter.IsClosed
                ? cut with { Parameter = NormalizePeriodic(cut.Parameter, adapter.Period) }
                : cut with { Parameter = Math.Clamp(cut.Parameter, adapter.StartParameter, adapter.EndParameter) })
            .OrderBy(cut => cut.Parameter)
            .ToList();

        var result = new List<CurveCut>();

        foreach (CurveCut cut in sortedCuts)
        {
            if (result.Count > 0 &&
                Math.Abs(result[^1].Parameter - cut.Parameter) <= tolerance.Parameter)
            {
                continue;
            }

            result.Add(cut);
        }

        if (adapter.IsClosed &&
            result.Count > 1 &&
            Math.Abs(result[0].Parameter + adapter.Period - result[^1].Parameter) <= tolerance.Parameter)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    private static int FindOpenIntervalContaining(
        IReadOnlyList<CurveCut> cuts,
        double parameter,
        GeometryTolerance tolerance)
    {
        for (int index = 0; index < cuts.Count - 1; index++)
        {
            if (parameter >= cuts[index].Parameter - tolerance.Parameter &&
                parameter <= cuts[index + 1].Parameter + tolerance.Parameter)
            {
                return index;
            }
        }

        return parameter < cuts[0].Parameter
            ? 0
            : cuts.Count - 2;
    }

    private static double NormalizePeriodic(
        double parameter,
        double period)
    {
        double value = parameter % period;
        return value < 0.0 ? value + period : value;
    }
}
