namespace OpenCad2D.Persistence.Dto;

/// <summary>
/// Serializable application viewport state.
/// </summary>
public sealed class ViewportStateDto
{
    public double PanX { get; set; }

    public double PanY { get; set; }

    public double Zoom { get; set; } = 1.0;
}
