using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using System;
using System.IO;

namespace OpenCad2D.App.ViewModels.ImageReferences;

public sealed class ImageReferenceItemViewModel
{
    public ImageReferenceItemViewModel(
        ImageReferenceEntity imageReference,
        int instanceCount)
    {
        ArgumentNullException.ThrowIfNull(imageReference);

        EntityId = imageReference.Id;
        FilePath = imageReference.FilePath;
        FileName = string.IsNullOrWhiteSpace(imageReference.FilePath)
            ? "<empty>"
            : Path.GetFileName(imageReference.FilePath);
        DirectoryPath = Path.GetDirectoryName(imageReference.FilePath) ?? string.Empty;
        Exists = File.Exists(imageReference.FilePath);
        StatusText = Exists ? "OK" : "Missing";
        PixelSizeText = imageReference.PixelWidth > 0 && imageReference.PixelHeight > 0
            ? $"{imageReference.PixelWidth} × {imageReference.PixelHeight} px"
            : "Unknown";
        CadSizeText = $"{imageReference.Width:0.###} × {imageReference.Height:0.###}";
        RotationText = $"{imageReference.RotationDegrees:0.###}°";
        InstanceCount = Math.Max(1, instanceCount);
        InstanceCountText = InstanceCount.ToString();
        IsMissing = !Exists;
    }

    public EntityId EntityId { get; }

    public string FilePath { get; }

    public string FileName { get; }

    public string DirectoryPath { get; }

    public bool Exists { get; }

    public bool IsMissing { get; }

    public string StatusText { get; }

    public string PixelSizeText { get; }

    public string CadSizeText { get; }

    public string RotationText { get; }

    public int InstanceCount { get; }

    public string InstanceCountText { get; }
}
