namespace OpenCad2D.Tools.Tests;

/// <summary>
/// Serializes test classes that execute Rotate or Scale: both tools remember the last angle,
/// reference angle, factor and reference length in static, session-wide defaults.
/// </summary>
[CollectionDefinition(Name)]
public sealed class TransformToolDefaultsCollection
{
    public const string Name = "Transform tool defaults";
}
