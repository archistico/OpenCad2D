using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Resolves textual command names and aliases to registered CAD tools.
/// </summary>
public sealed class CommandAliasRegistry
{
    private readonly Dictionary<string, ToolId> _aliases;

    public CommandAliasRegistry(IEnumerable<CommandAlias> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);

        _aliases = new Dictionary<string, ToolId>(StringComparer.OrdinalIgnoreCase);

        foreach (CommandAlias alias in aliases)
        {
            Register(alias);
        }
    }

    public IReadOnlyDictionary<string, ToolId> Aliases => _aliases;

    public static CommandAliasRegistry CreateDefault()
    {
        return new CommandAliasRegistry(CreateDefaultAliases());
    }

    public bool TryResolve(
        string? input,
        out ToolId toolId)
    {
        toolId = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string normalized = Normalize(input);

        return _aliases.TryGetValue(
            normalized,
            out toolId);
    }

    public bool Contains(string? input)
    {
        return TryResolve(input, out _);
    }

    private void Register(CommandAlias alias)
    {
        string name = Normalize(alias.Name);

        if (_aliases.ContainsKey(name))
        {
            throw new InvalidOperationException(
                $"Command alias '{name}' is already registered.");
        }

        _aliases.Add(
            name,
            alias.ToolId);
    }

    private static string Normalize(string input)
    {
        return input.Trim().ToUpperInvariant();
    }

    private static IEnumerable<CommandAlias> CreateDefaultAliases()
    {
        yield return new CommandAlias("SELECT", ToolId.Selection);
        yield return new CommandAlias("SEL", ToolId.Selection);

        yield return new CommandAlias("ZOOMWINDOW", ToolId.ZoomWindow);
        yield return new CommandAlias("ZW", ToolId.ZoomWindow);

        yield return new CommandAlias("POINT", ToolId.Point);
        yield return new CommandAlias("PO", ToolId.Point);

        yield return new CommandAlias("TEXT", ToolId.Text);
        yield return new CommandAlias("T", ToolId.Text);
        yield return new CommandAlias("MTEXT", ToolId.MultilineText);
        yield return new CommandAlias("MT", ToolId.MultilineText);

        yield return new CommandAlias("LINE", ToolId.Line);
        yield return new CommandAlias("L", ToolId.Line);

        yield return new CommandAlias("POLYLINE", ToolId.Polyline);
        yield return new CommandAlias("PL", ToolId.Polyline);

        yield return new CommandAlias("SPLINE", ToolId.Spline);
        yield return new CommandAlias("SPL", ToolId.Spline);

        yield return new CommandAlias("POLYGON", ToolId.Polygon);
        yield return new CommandAlias("PG", ToolId.Polygon);

        yield return new CommandAlias("RECTANGLE", ToolId.Rectangle);
        yield return new CommandAlias("REC", ToolId.Rectangle);

        yield return new CommandAlias("RECTANGLESIDES", ToolId.RectangleBySides);
        yield return new CommandAlias("RECTSIDES", ToolId.RectangleBySides);
        yield return new CommandAlias("RSIDES", ToolId.RectangleBySides);

        yield return new CommandAlias("CIRCLE", ToolId.Circle);
        yield return new CommandAlias("C", ToolId.Circle);

        yield return new CommandAlias("ELLIPSE", ToolId.Ellipse);
        yield return new CommandAlias("EL", ToolId.Ellipse);

        yield return new CommandAlias("ARC", ToolId.Arc);
        yield return new CommandAlias("A", ToolId.Arc);

        yield return new CommandAlias("ARC3P", ToolId.ArcThreePoints);
        yield return new CommandAlias("A3P", ToolId.ArcThreePoints);

        yield return new CommandAlias("HDIM", ToolId.HorizontalDimension);
        yield return new CommandAlias("H", ToolId.HorizontalDimension);
        yield return new CommandAlias("HORIZONTALDIM", ToolId.HorizontalDimension);
        yield return new CommandAlias("HORIZONTALDIMENSION", ToolId.HorizontalDimension);

        yield return new CommandAlias("VDIM", ToolId.VerticalDimension);
        yield return new CommandAlias("V", ToolId.VerticalDimension);
        yield return new CommandAlias("VERTICALDIM", ToolId.VerticalDimension);
        yield return new CommandAlias("VERTICALDIMENSION", ToolId.VerticalDimension);

        yield return new CommandAlias("ADIM", ToolId.AlignedDimension);
        yield return new CommandAlias("AL", ToolId.AlignedDimension);
        yield return new CommandAlias("ALIGNEDDIM", ToolId.AlignedDimension);
        yield return new CommandAlias("ALIGNEDDIMENSION", ToolId.AlignedDimension);

        yield return new CommandAlias("RDIM", ToolId.RadiusDimension);
        yield return new CommandAlias("RAD", ToolId.RadiusDimension);
        yield return new CommandAlias("RADIUSDIM", ToolId.RadiusDimension);
        yield return new CommandAlias("RADIUSDIMENSION", ToolId.RadiusDimension);

        yield return new CommandAlias("DDIM", ToolId.DiameterDimension);
        yield return new CommandAlias("DIA", ToolId.DiameterDimension);
        yield return new CommandAlias("DIAMETERDIM", ToolId.DiameterDimension);
        yield return new CommandAlias("DIAMETERDIMENSION", ToolId.DiameterDimension);

        yield return new CommandAlias("ANGDIM", ToolId.AngularDimension);
        yield return new CommandAlias("ANG", ToolId.AngularDimension);
        yield return new CommandAlias("ANGULARDIM", ToolId.AngularDimension);
        yield return new CommandAlias("ANGULARDIMENSION", ToolId.AngularDimension);

        yield return new CommandAlias("MOVE", ToolId.Move);
        yield return new CommandAlias("M", ToolId.Move);

        yield return new CommandAlias("COPY", ToolId.Copy);
        yield return new CommandAlias("CO", ToolId.Copy);

        yield return new CommandAlias("ROTATE", ToolId.Rotate);
        yield return new CommandAlias("RO", ToolId.Rotate);

        yield return new CommandAlias("SCALE", ToolId.Scale);
        yield return new CommandAlias("SC", ToolId.Scale);

        yield return new CommandAlias("ALIGN", ToolId.Align);

        yield return new CommandAlias("TRIM", ToolId.Trim);
        yield return new CommandAlias("TR", ToolId.Trim);

        yield return new CommandAlias("OFFSET", ToolId.Offset);
        yield return new CommandAlias("O", ToolId.Offset);

        yield return new CommandAlias("FILLET", ToolId.Fillet);
        yield return new CommandAlias("F", ToolId.Fillet);

        yield return new CommandAlias("MIRROR", ToolId.Mirror);
        yield return new CommandAlias("MI", ToolId.Mirror);

        yield return new CommandAlias("EXTEND", ToolId.Extend);
        yield return new CommandAlias("EX", ToolId.Extend);

        yield return new CommandAlias("BREAKPOINT", ToolId.BreakAtPoint);
        yield return new CommandAlias("BP", ToolId.BreakAtPoint);

        yield return new CommandAlias("BREAKSEGMENT", ToolId.BreakBetweenPoints);
        yield return new CommandAlias("BREAK", ToolId.BreakBetweenPoints);
        yield return new CommandAlias("BR", ToolId.BreakBetweenPoints);
        yield return new CommandAlias("BS", ToolId.BreakBetweenPoints);

        yield return new CommandAlias("DELETE", ToolId.Delete);
        yield return new CommandAlias("DEL", ToolId.Delete);

        yield return new CommandAlias("DISTANCE", ToolId.MeasureDistance);
        yield return new CommandAlias("DI", ToolId.MeasureDistance);
        yield return new CommandAlias("MEASUREDISTANCE", ToolId.MeasureDistance);

        yield return new CommandAlias("MEASURE", ToolId.MeasureEntity);
        yield return new CommandAlias("ME", ToolId.MeasureEntity);
        yield return new CommandAlias("MEASUREENTITY", ToolId.MeasureEntity);

        yield return new CommandAlias("MEASUREANGLE", ToolId.MeasureAngle);
        yield return new CommandAlias("MANG", ToolId.MeasureAngle);

        yield return new CommandAlias("MEASUREAREA", ToolId.MeasureArea);
        yield return new CommandAlias("MAREA", ToolId.MeasureArea);
    }
}
