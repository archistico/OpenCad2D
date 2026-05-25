namespace OpenCad2D.App.ViewModels.ImageReferences;

public sealed class ImageReferenceManagerResult
{
    public ImageReferenceManagerResult(
        ImageReferenceManagerAction action,
        ImageReferenceItemViewModel? reference)
    {
        Action = action;
        Reference = reference;
    }

    public ImageReferenceManagerAction Action { get; }

    public ImageReferenceItemViewModel? Reference { get; }
}
