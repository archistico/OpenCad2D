namespace OpenCad2D.Tools.Common;

/// <summary>
/// Defines the polar tracking angular constraint used by interactive tools.
/// </summary>
public sealed class AngleConstraintSettings
{
    private const double DefaultDisabledStepDegrees = 90.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="AngleConstraintSettings"/> class.
    /// </summary>
    /// <param name="isEnabled">True when polar tracking is enabled.</param>
    /// <param name="stepDegrees">The angular step, in degrees, used when polar tracking is enabled.</param>
    public AngleConstraintSettings(
        bool isEnabled,
        double stepDegrees)
    {
        if (isEnabled)
        {
            ValidateStepDegrees(stepDegrees);
        }

        IsEnabled = isEnabled;
        StepDegrees = isEnabled
            ? stepDegrees
            : DefaultDisabledStepDegrees;
    }

    /// <summary>
    /// Gets a value indicating whether polar tracking is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the angular step, in degrees.
    /// </summary>
    public double StepDegrees { get; }

    /// <summary>
    /// Gets disabled polar tracking settings.
    /// </summary>
    public static AngleConstraintSettings Off => new(false, DefaultDisabledStepDegrees);

    /// <summary>
    /// Creates enabled polar tracking settings with the specified angular step.
    /// </summary>
    /// <param name="stepDegrees">The angular step, in degrees.</param>
    /// <returns>The enabled polar tracking settings.</returns>
    public static AngleConstraintSettings FromStep(double stepDegrees) =>
        new(true, stepDegrees);

    private static void ValidateStepDegrees(double stepDegrees)
    {
        if (!double.IsFinite(stepDegrees) ||
            stepDegrees <= 0 ||
            stepDegrees > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepDegrees),
                stepDegrees,
                "Angle constraint step must be greater than 0 and less than or equal to 180 degrees.");
        }
    }
}
