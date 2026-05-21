namespace OpenCad2D.Core.Dimensions;

/// <summary>
/// Controls whether dimension terminators are drawn inside or outside short measured spans.
/// </summary>
public enum DimensionTerminatorFitMode
{
    /// <summary>
    /// Draw terminators inside the measured span.
    /// </summary>
    Inside,

    /// <summary>
    /// Draw terminators inside when there is enough room; otherwise draw them outside.
    /// </summary>
    OutsideWhenNeeded,

    /// <summary>
    /// Always draw terminators outside the measured span.
    /// </summary>
    AlwaysOutside
}
