using OpenCad2D.Tools.Architectural;
using OpenCad2D.Tools.Dimensions;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Measurements;
using OpenCad2D.Tools.Navigation;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Central registry used to describe and create CAD tools.
/// </summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<ToolId, Func<ICadTool>> _factories;
    private readonly Dictionary<ToolId, ToolDescriptor> _descriptors;

    public ToolRegistry(ITextInputProvider? textInputProvider = null)
    {
        _factories = new Dictionary<ToolId, Func<ICadTool>>();
        _descriptors = new Dictionary<ToolId, ToolDescriptor>();

        Register(
            new ToolDescriptor(
                ToolId.Selection,
                "Selection",
                "Selection",
                "Modify"),
            () => new SelectionTool());

        Register(
            new ToolDescriptor(
                ToolId.ZoomWindow,
                "ZoomWindow",
                "Zoom Window",
                "Navigation"),
            () => new ZoomWindowTool());

        Register(
            new ToolDescriptor(
                ToolId.Point,
                "Point",
                "Point",
                "Draw"),
            () => new PointTool());

        Register(
            new ToolDescriptor(
                ToolId.Divide,
                "Divide",
                "Divide",
                "Draw"),
            () => new DivideTool());

        Register(
            new ToolDescriptor(
                ToolId.Text,
                "Text",
                "Text",
                "Draw"),
            () => new TextTool(textInputProvider));

        Register(
            new ToolDescriptor(
                ToolId.MultilineText,
                "MultilineText",
                "MText",
                "Draw"),
            () => new MultilineTextTool(textInputProvider));

        Register(
            new ToolDescriptor(
                ToolId.Line,
                "Line",
                "Line",
                "Draw"),
            () => new LineTool());

        Register(
            new ToolDescriptor(
                ToolId.Rectangle,
                "Rectangle",
                "Rectangle",
                "Draw"),
            () => new RectangleTool());

        Register(
            new ToolDescriptor(
                ToolId.RectangleBySides,
                "RectangleBySides",
                "Rect Sides",
                "Draw"),
            () => new RectangleBySidesTool());

        Register(
            new ToolDescriptor(
                ToolId.Circle,
                "Circle",
                "Circle",
                "Draw"),
            () => new CircleTool());

        Register(
            new ToolDescriptor(
                ToolId.Ellipse,
                "Ellipse",
                "Ellipse",
                "Draw"),
            () => new EllipseTool());

        Register(
            new ToolDescriptor(
                ToolId.Arc,
                "Arc",
                "Arc",
                "Draw"),
            () => new ArcTool());

        Register(
            new ToolDescriptor(
                ToolId.ArcThreePoints,
                "ArcThreePoints",
                "Arc 3P",
                "Draw"),
            () => new ArcThreePointsTool());

        Register(
            new ToolDescriptor(
                ToolId.Polyline,
                "Polyline",
                "Polyline",
                "Draw"),
            () => new PolylineTool());

        Register(
            new ToolDescriptor(
                ToolId.Spline,
                "Spline",
                "Spline",
                "Draw"),
            () => new SplineTool());

        Register(
            new ToolDescriptor(
                ToolId.Polygon,
                "Polygon",
                "Polygon",
                "Draw"),
            () => new PolygonTool());

        Register(
            new ToolDescriptor(
                ToolId.NorthSymbol,
                "NorthSymbol",
                "North Symbol",
                "Symbols"),
            () => new NorthSymbolTool());

        Register(
            new ToolDescriptor(
                ToolId.ScaleBar,
                "ScaleBar",
                "Metric Scale Bar",
                "Symbols"),
            () => new ScaleBarTool());

        Register(
            new ToolDescriptor(
                ToolId.Stair,
                "Stair",
                "Stair",
                "Symbols"),
            () => new StairTool());


        Register(
            new ToolDescriptor(
                ToolId.HorizontalDimension,
                "HorizontalDimension",
                "Horizontal Dim",
                "Dimension"),
            () => new HorizontalDimensionTool());

        Register(
            new ToolDescriptor(
                ToolId.VerticalDimension,
                "VerticalDimension",
                "Vertical Dim",
                "Dimension"),
            () => new VerticalDimensionTool());

        Register(
            new ToolDescriptor(
                ToolId.AlignedDimension,
                "AlignedDimension",
                "Aligned Dim",
                "Dimension"),
            () => new AlignedDimensionTool());

        Register(
            new ToolDescriptor(
                ToolId.RadiusDimension,
                "RadiusDimension",
                "Radius Dim",
                "Dimension"),
            () => new RadiusDimensionTool());

        Register(
            new ToolDescriptor(
                ToolId.DiameterDimension,
                "DiameterDimension",
                "Diameter Dim",
                "Dimension"),
            () => new DiameterDimensionTool());

        Register(
            new ToolDescriptor(
                ToolId.AngularDimension,
                "AngularDimension",
                "Angular Dim",
                "Dimension"),
            () => new AngularDimensionTool());

        Register(
            new ToolDescriptor(
                ToolId.Move,
                "Move",
                "Move",
                "Modify"),
            () => new MoveTool());

        Register(
            new ToolDescriptor(
                ToolId.Copy,
                "Copy",
                "Copy",
                "Modify"),
            () => new CopyTool());

        Register(
            new ToolDescriptor(
                ToolId.Rotate,
                "Rotate",
                "Rotate",
                "Modify"),
            () => new RotateTool());

        Register(
            new ToolDescriptor(
                ToolId.Scale,
                "Scale",
                "Scale",
                "Modify"),
            () => new ScaleTool());

        Register(
            new ToolDescriptor(
                ToolId.Align,
                "Align",
                "Align",
                "Modify"),
            () => new AlignTool());

        Register(
            new ToolDescriptor(
                ToolId.BreakAtPoint,
                "BreakAtPoint",
                "Break Point",
                "Modify"),
            () => new BreakAtPointTool());

        Register(
            new ToolDescriptor(
                ToolId.BreakBetweenPoints,
                "BreakBetweenPoints",
                "Break Segment",
                "Modify"),
            () => new BreakBetweenPointsTool());

        Register(
            new ToolDescriptor(
                ToolId.Extend,
                "Extend",
                "Extend",
                "Modify"),
            () => new ExtendTool());

        Register(
            new ToolDescriptor(
                ToolId.Trim,
                "Trim",
                "Trim",
                "Modify"),
            () => new TrimTool());

        Register(
            new ToolDescriptor(
                ToolId.Offset,
                "Offset",
                "Offset",
                "Modify"),
            () => new OffsetTool());

        Register(
            new ToolDescriptor(
                ToolId.BoundaryFill,
                "BoundaryFill",
                "Boundary Fill",
                "Modify"),
            () => new BoundaryFillTool());

        Register(
            new ToolDescriptor(
                ToolId.Fillet,
                "Fillet",
                "Fillet",
                "Modify"),
            () => new FilletTool());

        Register(
            new ToolDescriptor(
                ToolId.Chamfer,
                "Chamfer",
                "Chamfer",
                "Modify"),
            () => new ChamferTool());

        Register(
            new ToolDescriptor(
                ToolId.Mirror,
                "Mirror",
                "Mirror",
                "Modify"),
            () => new MirrorTool());

        Register(
            new ToolDescriptor(
                ToolId.Explode,
                "Explode",
                "Explode",
                "Modify"),
            () => new ExplodeTool());

        Register(
            new ToolDescriptor(
                ToolId.Join,
                "Join",
                "Join",
                "Modify"),
            () => new JoinTool());

        Register(
            new ToolDescriptor(
                ToolId.Delete,
                "Delete",
                "Delete",
                "Modify"),
            () => new DeleteTool());

        Register(
            new ToolDescriptor(
                ToolId.MeasureDistance,
                "MeasureDistance",
                "Distance",
                "Measure"),
            () => new MeasureDistanceTool());

        Register(
            new ToolDescriptor(
                ToolId.MeasureEntity,
                "MeasureEntity",
                "Entity",
                "Measure"),
            () => new MeasureEntityTool());

        Register(
            new ToolDescriptor(
                ToolId.MeasureAngle,
                "MeasureAngle",
                "Angle",
                "Measure"),
            () => new MeasureAngleTool());

        Register(
            new ToolDescriptor(
                ToolId.MeasureArea,
                "MeasureArea",
                "Area",
                "Measure"),
            () => new MeasureAreaTool());
    }

    public IReadOnlyList<ToolDescriptor> Tools =>
        _descriptors.Values
            .OrderBy(descriptor => descriptor.Category)
            .ThenBy(descriptor => descriptor.DisplayName)
            .ToList();

    public IReadOnlyList<ToolDescriptor> GetByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException(
                "Category cannot be empty.",
                nameof(category));
        }

        return _descriptors.Values
            .Where(descriptor => string.Equals(
                descriptor.Category,
                category,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(descriptor => descriptor.DisplayName)
            .ToList();
    }

    public bool Contains(ToolId id)
    {
        return _factories.ContainsKey(id);
    }

    public ToolDescriptor GetDescriptor(ToolId id)
    {
        if (!_descriptors.TryGetValue(id, out ToolDescriptor? descriptor))
        {
            throw new KeyNotFoundException(
                $"Tool descriptor '{id}' was not found.");
        }

        return descriptor;
    }

    public ICadTool Create(ToolId id)
    {
        if (!_factories.TryGetValue(id, out Func<ICadTool>? factory))
        {
            throw new KeyNotFoundException(
                $"Tool factory '{id}' was not found.");
        }

        return factory();
    }

    private void Register(
        ToolDescriptor descriptor,
        Func<ICadTool> factory)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(factory);

        if (_factories.ContainsKey(descriptor.Id))
        {
            throw new InvalidOperationException(
                $"Tool '{descriptor.Id}' is already registered.");
        }

        _descriptors.Add(
            descriptor.Id,
            descriptor);

        _factories.Add(
            descriptor.Id,
            factory);
    }
}
