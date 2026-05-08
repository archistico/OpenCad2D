using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using System;

namespace OpenCad2D.App.Controls;

public sealed class CadCanvasWorkspaceChangedEventArgs : EventArgs
{
    public CadCanvasWorkspaceChangedEventArgs(
        ToolResult result,
        Point2D mousePosition)
    {
        Result = result;
        MousePosition = mousePosition;
    }

    public ToolResult Result { get; }

    public Point2D MousePosition { get; }
}