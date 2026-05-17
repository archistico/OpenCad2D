using OpenCad2D.App.Diagnostics;

namespace OpenCad2D.App.Tests;

public sealed class ApplicationLoggerTests
{
    [Fact]
    public void InMemoryLogger_Error_ShouldStoreExceptionDetails()
    {
        var logger = new InMemoryApplicationLogger();
        var exception = new InvalidOperationException("Preview failed");

        logger.Error("CadCanvas", "Unhandled exception while processing pointer input.", exception);

        ApplicationLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(ApplicationLogLevel.Error, entry.Level);
        Assert.Equal("CadCanvas", entry.Category);
        Assert.Equal("Unhandled exception while processing pointer input.", entry.Message);
        Assert.Same(exception, entry.Exception);
        string exceptionDetails = Assert.IsType<string>(entry.ExceptionDetails);
        Assert.Contains("Preview failed", exceptionDetails);
        Assert.Contains(nameof(InvalidOperationException), exceptionDetails);
    }

    [Fact]
    public void InMemoryLogger_InfoAndWarning_ShouldStoreEntriesInOrder()
    {
        var logger = new InMemoryApplicationLogger();

        logger.Info("Startup", "Application started.");
        logger.Warning("DxfImport", "Unsupported entity skipped.");

        IReadOnlyList<ApplicationLogEntry> entries = logger.Entries;
        Assert.Equal(2, entries.Count);
        Assert.Equal(ApplicationLogLevel.Info, entries[0].Level);
        Assert.Equal("Startup", entries[0].Category);
        Assert.Equal(ApplicationLogLevel.Warning, entries[1].Level);
        Assert.Equal("DxfImport", entries[1].Category);
    }

    [Fact]
    public void ApplicationLogEntry_WhenCategoryIsEmpty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new ApplicationLogEntry(
                DateTimeOffset.Now,
                ApplicationLogLevel.Info,
                string.Empty,
                "Message"));
    }

    [Fact]
    public void ApplicationLogEntry_WhenMessageIsEmpty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new ApplicationLogEntry(
                DateTimeOffset.Now,
                ApplicationLogLevel.Info,
                "Category",
                string.Empty));
    }
}
