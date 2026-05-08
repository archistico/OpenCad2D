using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using System;

namespace OpenCad2D.App.Controls;

public sealed class CadCanvasWorkspaceChangedEventArgs : EventArgs
{
    public CadCanvasWorkspaceChangedEventArgs(
        ToolResult result,
        Point2D mousePosition,
        SnapCandidate? snapCandidate = null)
    {
        Result = result;
        MousePosition = mousePosition;
        SnapCandidate = snapCandidate;
    }

    public ToolResult Result { get; }

    public Point2D MousePosition { get; }

    public SnapCandidate? SnapCandidate { get; }
}