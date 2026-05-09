using OpenCad2D.Core.Entities;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Resolves the grip provider that supports a given CAD entity.
/// </summary>
public sealed class GripProviderRegistry
{
    private readonly List<IGripProvider> _providers = new();

    public GripProviderRegistry()
    {
        Register(new LineGripProvider());
        Register(new CircleGripProvider());
    }

    public IReadOnlyList<IGripProvider> Providers => _providers;

    public void Register(IGripProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _providers.Add(provider);
    }

    public IGripProvider? FindProvider(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return _providers.FirstOrDefault(provider => provider.CanHandle(entity));
    }
}
