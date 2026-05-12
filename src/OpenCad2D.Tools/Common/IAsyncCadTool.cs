using System.Threading.Tasks;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Optional asynchronous extension point for tools that need non-blocking UI interaction.
/// </summary>
public interface IAsyncCadTool : ICadTool
{
    Task<ToolResult> OnPointerPressedAsync(
        ToolContext context,
        PointerInfo pointer);
}
