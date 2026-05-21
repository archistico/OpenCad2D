using OpenCad2D.Interaction.Snapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenCad2D.App.Settings;

public sealed class ApplicationSettings
{
    public const int CurrentSchemaVersion = 2;
    public const int DefaultRecentFileLimit = 10;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string? LastOpenedFilePath { get; set; }

    public string? LastOpenDirectory { get; set; }

    public string? LastSaveDirectory { get; set; }

    public string? LastExportDirectory { get; set; }

    public bool ReopenLastFileOnStartup { get; set; }

    public ApplicationGridSettings? DefaultGrid { get; set; }

    public ApplicationSnapSettings? DefaultSnapping { get; set; }

    public List<string> RecentFiles { get; set; } = new();

    public bool HasLastOpenedFile => !string.IsNullOrWhiteSpace(LastOpenedFilePath);

    public ApplicationSettings Normalize()
    {
        SchemaVersion = CurrentSchemaVersion;
        LastOpenedFilePath = NormalizePath(LastOpenedFilePath);
        LastOpenDirectory = NormalizeDirectory(LastOpenDirectory);
        LastSaveDirectory = NormalizeDirectory(LastSaveDirectory);
        LastExportDirectory = NormalizeDirectory(LastExportDirectory);
        DefaultGrid = DefaultGrid?.Normalize();
        DefaultSnapping = DefaultSnapping?.Normalize();
        RecentFiles = NormalizeRecentFiles(RecentFiles);
        return this;
    }

    public ApplicationSettings RegisterOpenedFile(string filePath)
    {
        string? normalizedPath = NormalizePath(filePath);

        if (normalizedPath is null)
        {
            return Normalize();
        }

        LastOpenedFilePath = normalizedPath;
        LastOpenDirectory = NormalizeDirectory(Path.GetDirectoryName(normalizedPath));
        RegisterRecentFile(normalizedPath);
        return Normalize();
    }

    public ApplicationSettings RegisterSavedFile(string filePath)
    {
        string? normalizedPath = NormalizePath(filePath);

        if (normalizedPath is null)
        {
            return Normalize();
        }

        LastSaveDirectory = NormalizeDirectory(Path.GetDirectoryName(normalizedPath));
        RegisterRecentFile(normalizedPath);
        return Normalize();
    }

    public ApplicationSettings RegisterExportedFile(string filePath)
    {
        string? normalizedPath = NormalizePath(filePath);

        if (normalizedPath is null)
        {
            return Normalize();
        }

        LastExportDirectory = NormalizeDirectory(Path.GetDirectoryName(normalizedPath));
        return Normalize();
    }

    public ApplicationSettings CaptureDraftingDefaults(
        GridSettings gridSettings,
        SnapKind enabledSnaps,
        double snapTolerance)
    {
        ArgumentNullException.ThrowIfNull(gridSettings);

        DefaultGrid = ApplicationGridSettings.FromGridSettings(gridSettings);
        DefaultSnapping = ApplicationSnapSettings.FromSnapSettings(
            enabledSnaps,
            snapTolerance);
        return Normalize();
    }

    private void RegisterRecentFile(string filePath)
    {
        RecentFiles.RemoveAll(existing => string.Equals(
            existing,
            filePath,
            StringComparison.OrdinalIgnoreCase));

        RecentFiles.Insert(0, filePath);
    }

    private static List<string> NormalizeRecentFiles(IEnumerable<string>? paths)
    {
        if (paths is null)
        {
            return new List<string>();
        }

        return paths
            .Select(NormalizePath)
            .Where(path => path is not null)
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(DefaultRecentFileLimit)
            .ToList();
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path.Trim();
    }

    private static string? NormalizeDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path.Trim();
    }
}
