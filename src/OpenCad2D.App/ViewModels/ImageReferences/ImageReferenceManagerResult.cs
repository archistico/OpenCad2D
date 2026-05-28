namespace OpenCad2D.App.ViewModels.ImageReferences;

public sealed class ImageReferenceManagerResult
{
    public ImageReferenceManagerResult(
        ImageReferenceManagerAction action,
        ImageReferenceItemViewModel? reference,
        double? transparencyPercent = null)
    {
        Action = action;
        Reference = reference;
        TransparencyPercent = transparencyPercent;
    }

    public ImageReferenceManagerAction Action { get; }

    public ImageReferenceItemViewModel? Reference { get; }

    public double? TransparencyPercent { get; }
}
