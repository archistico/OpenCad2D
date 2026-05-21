namespace OpenCad2D.Core.Dimensions;

/// <summary>
/// Controls where dimension text is placed when the measured span is too short.
/// </summary>
public enum DimensionTextFitMode
{
    /// <summary>
    /// Keep text at the dimension line midpoint.
    /// </summary>
    Inside,

    /// <summary>
    /// Keep text inside when there is enough room; otherwise place it outside.
    /// </summary>
    OutsideWhenNeeded,

    /// <summary>
    /// Always place text outside the measured span.
    /// </summary>
    AlwaysOutside
}
