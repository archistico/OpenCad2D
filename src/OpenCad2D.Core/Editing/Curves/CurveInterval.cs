namespace OpenCad2D.Core.Editing.Curves;

/// <summary>
/// Represents a kept interval on a native curve.
/// </summary>
public readonly record struct CurveInterval(
    CurveCut Start,
    CurveCut End);
