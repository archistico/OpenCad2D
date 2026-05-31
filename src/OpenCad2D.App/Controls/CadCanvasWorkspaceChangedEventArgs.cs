using Avalonia;
using Avalonia.Input;
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
        SnapCandidate? snapCandidate = null,
        bool isPointerPressed = false,
        Point? pointerScreenPosition = null,
        KeyModifiers keyModifiers = KeyModifiers.None)
    {
        Result = result;
        MousePosition = mousePosition;
        SnapCandidate = snapCandidate;
        IsPointerPressed = isPointerPressed;
        PointerScreenPosition = pointerScreenPosition;
        KeyModifiers = keyModifiers;
    }

    public ToolResult Result { get; }

    public Point2D MousePosition { get; }

    public SnapCandidate? SnapCandidate { get; }

    public bool IsPointerPressed { get; }

    public Point? PointerScreenPosition { get; }

    public KeyModifiers KeyModifiers { get; }
}

