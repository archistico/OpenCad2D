namespace OpenCad2D.App.Settings;

public interface IApplicationSettingsStore
{
    ApplicationSettings Load();

    void Save(ApplicationSettings settings);
}
