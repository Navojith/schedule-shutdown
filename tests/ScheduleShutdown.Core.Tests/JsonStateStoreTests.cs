using FluentAssertions;
using ScheduleShutdown.Core.Models;
using ScheduleShutdown.Core.Persistence;

namespace ScheduleShutdown.Core.Tests;

public sealed class JsonStateStoreTests : IDisposable
{
    private static readonly ScheduleState Sample =
        new(new DateTimeOffset(2026, 10, 30, 23, 30, 0, TimeSpan.FromHours(5.5)), ScheduleMode.AtTime, new ShutdownOptions(Force: true));

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ScheduleShutdownTests", Guid.NewGuid().ToString("N"));
    private readonly List<string> _errors = [];
    private readonly JsonStateStore _store;

    public JsonStateStoreTests()
    {
        _store = new JsonStateStore(Path.Combine(_directory, "nested", "state.json"), _errors.Add);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Load_NoFile_ReturnsNullWithoutError()
    {
        _store.Load().Should().BeNull();
        _errors.Should().BeEmpty();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsIncludingOffset()
    {
        _store.Save(Sample);

        var loaded = _store.Load();

        loaded.Should().Be(Sample);
        loaded!.Target.Offset.Should().Be(Sample.Target.Offset);
        _errors.Should().BeEmpty();
    }

    [Fact]
    public void Save_CreatesDirectoryAndLeavesNoTempFile()
    {
        _store.Save(Sample);

        File.Exists(_store.Path).Should().BeTrue();
        File.Exists(_store.Path + ".tmp").Should().BeFalse();
    }

    [Fact]
    public void Save_OverwritesPreviousState()
    {
        _store.Save(Sample);
        var later = Sample with { Target = Sample.Target.AddMinutes(10), Mode = ScheduleMode.Timer, Options = ShutdownOptions.Default };

        _store.Save(later);

        _store.Load().Should().Be(later);
    }

    [Fact]
    public void Save_WritesReadableJsonWithEnumNames()
    {
        _store.Save(Sample);

        var json = File.ReadAllText(_store.Path);
        json.Should().Contain("\"version\": 1").And.Contain("\"mode\": \"AtTime\"").And.Contain("\"force\": true");
    }

    [Fact]
    public void Clear_DeletesFile()
    {
        _store.Save(Sample);

        _store.Clear();

        File.Exists(_store.Path).Should().BeFalse();
        _store.Load().Should().BeNull();
    }

    [Fact]
    public void Clear_NoFile_DoesNothing()
    {
        _store.Clear();

        _errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{ "version": 2, "target": "2026-10-30T23:30:00+05:30", "mode": "Timer", "force": false }""")]
    [InlineData("""{ "version": 1, "mode": "Timer", "force": false }""")]
    [InlineData("""{ "version": 1, "target": "2026-10-30T23:30:00+05:30", "force": false }""")]
    [InlineData("""{ "version": 1, "target": "2026-10-30T23:30:00+05:30", "mode": "Sleep", "force": false }""")]
    [InlineData("""{ "version": 1, "target": "2026-10-30T23:30:00+05:30", "mode": 7, "force": false }""")]
    [InlineData("""{ "version": 1, "target": "yesterday", "mode": "Timer", "force": false }""")]
    public void Load_InvalidContent_ReturnsNullAndReportsError(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_store.Path)!);
        File.WriteAllText(_store.Path, content);

        _store.Load().Should().BeNull();
        _errors.Should().ContainSingle();
    }

    [Fact]
    public void Load_MissingForce_DefaultsToOff()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_store.Path)!);
        File.WriteAllText(_store.Path, """{ "version": 1, "target": "2026-10-30T23:30:00+05:30", "mode": "Timer" }""");

        _store.Load()!.Options.Force.Should().BeFalse();
    }

    [Fact]
    public void Save_UnwritableLocation_ReportsErrorWithoutThrowing()
    {
        // A directory where the file should be makes the final move fail.
        Directory.CreateDirectory(_store.Path);

        var act = () => _store.Save(Sample);

        act.Should().NotThrow();
        _errors.Should().ContainSingle().Which.Should().StartWith("Couldn't save");
    }
}
