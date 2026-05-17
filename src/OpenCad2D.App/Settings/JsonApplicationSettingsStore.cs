using System;
using System.IO;
using System.Text.Json;

namespace OpenCad2D.App.Settings;

public sealed class JsonApplicationSettingsStore : IApplicationSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonApplicationSettingsStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Settings file path cannot be empty.",
                nameof(filePath));
        }

        _filePath = filePath;
    }

    public static JsonApplicationSettingsStore CreateDefault()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrWhiteSpace(root))
        {
            root = AppContext.BaseDirectory;
        }

        return new JsonApplicationSettingsStore(
            Path.Combine(
                root,
                "OpenCad2D",
                "settings.json"));
    }

    public ApplicationSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new ApplicationSettings();
            }

            string json = File.ReadAllText(_filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new ApplicationSettings();
            }

            ApplicationSettings? settings = JsonSerializer.Deserialize<ApplicationSettings>(
                json,
                SerializerOptions);

            return (settings ?? new ApplicationSettings()).Normalize();
        }
        catch (JsonException)
        {
            return new ApplicationSettings();
        }
        catch (IOException)
        {
            return new ApplicationSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new ApplicationSettings();
        }
    }

    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Normalize();

        string? directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(
            settings,
            SerializerOptions);

        File.WriteAllText(
            _filePath,
            json);
    }
}
