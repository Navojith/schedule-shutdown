using FluentAssertions;
using ScheduleShutdown.Core.Models;
using ScheduleShutdown.Core.Persistence;

namespace ScheduleShutdown.Core.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ScheduleShutdownTests", Guid.NewGuid().ToString("N"));
    private readonly List<string> _errors = [];
    private readonly JsonSettingsStore _store;

    public JsonSettingsStoreTests()
    {
        _store = new JsonSettingsStore(Path.Combine(_directory, "settings.json"), _errors.Add);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private void WriteFile(string content)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_store.Path, content);
    }

    [Fact]
    public void Load_NoFile_ReturnsDefaults()
    {
        _store.Load().Should().Be(AppSettings.Default);
        _errors.Should().BeEmpty();
    }

    [Fact]
    public void Default_FollowsSystemTheme()
    {
        AppSettings.Default.Theme.Should().Be(AppTheme.System);
    }

    [Theory]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    public void Save_ThenLoad_RoundTrips(AppTheme theme)
    {
        _store.Save(new AppSettings(theme));

        _store.Load().Should().Be(new AppSettings(theme));
        File.ReadAllText(_store.Path).Should().Contain($"\"theme\": \"{theme}\"");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public void Load_InvalidFile_ReturnsDefaultsAndReportsError(string content)
    {
        WriteFile(content);

        _store.Load().Should().Be(AppSettings.Default);
        _errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "theme": 9 }""")]
    public void Load_MissingOrUnknownTheme_FallsBackToSystem(string content)
    {
        WriteFile(content);

        _store.Load().Theme.Should().Be(AppTheme.System);
    }

    [Fact]
    public void Load_UnknownProperties_AreIgnored()
    {
        WriteFile("""{ "theme": "Dark", "fromTheFuture": true }""");

        _store.Load().Theme.Should().Be(AppTheme.Dark);
        _errors.Should().BeEmpty();
    }
}
