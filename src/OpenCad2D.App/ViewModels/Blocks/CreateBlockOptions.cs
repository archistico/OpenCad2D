namespace OpenCad2D.App.ViewModels.Blocks;

public sealed record CreateBlockOptions(
    string Name,
    double BasePointX,
    double BasePointY,
    bool PickBasePointFromDrawing = false,
    bool PickEntitiesFromDrawing = false);
