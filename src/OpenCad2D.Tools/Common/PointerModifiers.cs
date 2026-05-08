namespace OpenCad2D.Tools.Common;

/// <summary>
/// Keyboard modifiers active during pointer input.
/// </summary>
[Flags]
public enum PointerModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4
}