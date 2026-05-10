namespace OpenCad2D.App.ViewModels.Properties;

public sealed class PropertyRowViewModel
{
    public PropertyRowViewModel(
        string name,
        string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public string Value { get; }
}
