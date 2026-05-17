namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides a UI-agnostic preview descriptor for tools that need more than
/// simple transient entity previews.
/// </summary>
public interface IToolPreviewDescriptorProvider
{
    /// <summary>
    /// Gets the current transient preview descriptor for the tool.
    /// </summary>
    ToolPreviewDescriptor GetPreviewDescriptor(ToolContext context);
}
