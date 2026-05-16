using System.Collections.Generic;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.App.Rendering;

public readonly record struct EntityScreenStyle(
    CadColor Color,
    double LineWeight,
    LineStyle LineStyle,
    IReadOnlyList<double> DashPattern);
