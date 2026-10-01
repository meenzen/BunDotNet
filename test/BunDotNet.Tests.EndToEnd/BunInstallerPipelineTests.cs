namespace BunDotNet.Tests.EndToEnd;

/// <summary>
/// End-to-end tests that download and run real Bun binaries. The tests form a single pipeline that shares one
/// installation directory: install latest -> run -> install older -> run -> list -> cleanup -> run -> canary.
/// </summary>
[NotInParallel]
public class BunInstallerPipelineTests
{
    // 1.3.10 is the oldest version available on all supported platforms (Windows ARM64)
    private static readonly BunVersion OlderVersion = BunVersion.Parse("1.3.10")!;

    private static string _rootDirectory = null!;
    private static string _installPath = null!;
    private static string _workingDirectory = null!;
    private static BunRuntime _latest = null!;
    private static BunRuntime _older = null!;
    private static BunRuntime _canary = null!;

    [Before(Class)]
    public static void CreateDirectories()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "BunDotNet.Tests", Guid.NewGuid().ToString("N"));
        _installPath = Path.Combine(_rootDirectory, "install");
        _workingDirectory = Path.Combine(_rootDirectory, "work");
        Directory.CreateDirectory(_workingDirectory);
    }

    [After(Class)]
    public static void DeleteDirectories()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private static async Task<string> RunVersionScriptAsync(BunRuntime runtime)
    {
        var outputFile = Path.Combine(_workingDirectory, "version.txt");
        File.Delete(outputFile);
        await File.WriteAllTextAsync(
            Path.Combine(_workingDirectory, "version.ts"),
            "await Bun.write(\"version.txt\", Bun.version);\n"
        );

        var exitCode = await runtime.RunAsync(["run", "version.ts"], _workingDirectory);

        await Assert.That(exitCode).IsEqualTo(0);
        return await File.ReadAllTextAsync(outputFile);
    }

    [Test]
    public async Task InstallLatest()
    {
        var progressReported = false;

        _latest = await BunInstaller.InstallAsync(path: _installPath, onProgress: _ => progressReported = true);

        await Assert.That(File.Exists(_latest.ExecutablePath)).IsTrue();
        await Assert.That(_latest.ExecutablePath).StartsWith(_installPath);
        await Assert.That(_latest.Metadata.Version).IsGreaterThan(OlderVersion);
        await Assert.That(_latest.Metadata.SizeBytes).IsEqualTo(new FileInfo(_latest.ExecutablePath).Length);
        await Assert.That(progressReported).IsTrue();
    }

    [Test]
    [DependsOn(nameof(InstallLatest))]
    public async Task RunLatest()
    {
        var version = await RunVersionScriptAsync(_latest);

        await Assert.That(version).IsEqualTo(_latest.Metadata.Version.ToString());
    }

    [Test]
    [DependsOn(nameof(RunLatest))]
    public async Task RunReturnsExitCode()
    {
        var exitCode = await _latest.RunAsync(["-e", "process.exit(3)"], _workingDirectory);

        await Assert.That(exitCode).IsEqualTo(3);
    }

    [Test]
    [DependsOn(nameof(RunLatest))]
    public async Task InstallLatestAgain_IsIdempotent()
    {
        // the update check timestamp was saved by the first install, so this must not download again
        var progressReported = false;

        var runtime = await BunInstaller.InstallAsync(path: _installPath, onProgress: _ => progressReported = true);

        await Assert.That(runtime.Metadata.Version).IsEqualTo(_latest.Metadata.Version);
        await Assert.That(runtime.ExecutablePath).IsEqualTo(_latest.ExecutablePath);
        await Assert.That(progressReported).IsFalse();
    }

    [Test]
    [DependsOn(nameof(InstallLatestAgain_IsIdempotent))]
    [DependsOn(nameof(RunReturnsExitCode))]
    public async Task InstallOlder()
    {
        _older = await BunInstaller.InstallAsync(version: OlderVersion, path: _installPath);

        await Assert.That(_older.Metadata.Version).IsEqualTo(OlderVersion);
        await Assert.That(File.Exists(_older.ExecutablePath)).IsTrue();
        await Assert.That(_older.ExecutablePath).IsNotEqualTo(_latest.ExecutablePath);
    }

    [Test]
    [DependsOn(nameof(InstallOlder))]
    public async Task RunOlder()
    {
        var version = await RunVersionScriptAsync(_older);

        await Assert.That(version).IsEqualTo(OlderVersion.ToString());
    }

    [Test]
    [DependsOn(nameof(RunOlder))]
    public async Task ListVersions()
    {
        var versions = await BunInstaller.ListVersionsAsync(_installPath);

        await Assert
            .That(versions.Select(v => v.Metadata.Version))
            .IsEquivalentTo(
                [OlderVersion, _latest.Metadata.Version],
                EqualityComparer<BunVersion>.Default,
                TUnit.Assertions.Enums.CollectionOrdering.Matching
            );
        await Assert
            .That(versions.Select(v => v.ExecutablePath))
            .IsEquivalentTo(
                [_older.ExecutablePath, _latest.ExecutablePath],
                EqualityComparer<string>.Default,
                TUnit.Assertions.Enums.CollectionOrdering.Matching
            );
    }

    [Test]
    [DependsOn(nameof(ListVersions))]
    public async Task InstallWithoutVersion_UsesLatestInstalled()
    {
        var runtime = await BunInstaller.InstallAsync(path: _installPath);

        await Assert.That(runtime.Metadata.Version).IsEqualTo(_latest.Metadata.Version);
    }

    [Test]
    [DependsOn(nameof(InstallWithoutVersion_UsesLatestInstalled))]
    public async Task Cleanup_RemovesOlderVersions()
    {
        var result = await BunInstaller.CleanupAsync(_installPath);

        await Assert.That(result.RemovedVersions.Count).IsEqualTo(1);
        await Assert.That(result.RemovedVersions[0].Metadata.Version).IsEqualTo(OlderVersion);
        await Assert.That(File.Exists(_older.ExecutablePath)).IsFalse();
        await Assert.That(File.Exists(_latest.ExecutablePath)).IsTrue();

        var versions = await BunInstaller.ListVersionsAsync(_installPath);
        await Assert.That(versions.Count).IsEqualTo(1);
        await Assert.That(versions[0].Metadata.Version).IsEqualTo(_latest.Metadata.Version);
    }

    [Test]
    [DependsOn(nameof(Cleanup_RemovesOlderVersions))]
    public async Task Cleanup_WithSingleVersion_RemovesNothing()
    {
        var result = await BunInstaller.CleanupAsync(_installPath);

        await Assert.That(result.RemovedVersions).IsEmpty();
    }

    [Test]
    [DependsOn(nameof(Cleanup_WithSingleVersion_RemovesNothing))]
    public async Task RunLatestAfterCleanup()
    {
        var version = await RunVersionScriptAsync(_latest);

        await Assert.That(version).IsEqualTo(_latest.Metadata.Version.ToString());
    }

    [Test]
    [DependsOn(nameof(RunLatestAfterCleanup))]
    public async Task InstallCanary()
    {
        var progressReported = false;

        _canary = await BunInstaller.InstallAsync(
            version: BunVersion.Canary,
            path: _installPath,
            onProgress: _ => progressReported = true
        );

        await Assert.That(_canary.Metadata.Version).IsEqualTo(BunVersion.Canary);
        await Assert.That(File.Exists(_canary.ExecutablePath)).IsTrue();
        await Assert.That(_canary.ExecutablePath).IsNotEqualTo(_latest.ExecutablePath);
        await Assert.That(_canary.Metadata.DownloadUrl).Contains("/releases/download/canary/");
        await Assert.That(progressReported).IsTrue();
    }

    [Test]
    [DependsOn(nameof(InstallCanary))]
    public async Task RunCanary()
    {
        var exitCode = await _canary.RunAsync(["-e", "process.exit(Bun.revision ? 0 : 1)"], _workingDirectory);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    [DependsOn(nameof(RunCanary))]
    public async Task InstallCanaryAgain_IsIdempotent()
    {
        var progressReported = false;

        var runtime = await BunInstaller.InstallAsync(
            version: BunVersion.Canary,
            path: _installPath,
            onProgress: _ => progressReported = true
        );

        await Assert.That(runtime.ExecutablePath).IsEqualTo(_canary.ExecutablePath);
        await Assert.That(progressReported).IsFalse();
    }

    [Test]
    [DependsOn(nameof(InstallCanaryAgain_IsIdempotent))]
    public async Task InstallWithoutVersion_IgnoresCanary()
    {
        var runtime = await BunInstaller.InstallAsync(path: _installPath);

        await Assert.That(runtime.Metadata.Version).IsEqualTo(_latest.Metadata.Version);
    }

    [Test]
    [DependsOn(nameof(InstallWithoutVersion_IgnoresCanary))]
    public async Task Cleanup_KeepsCanary()
    {
        var result = await BunInstaller.CleanupAsync(_installPath);

        await Assert.That(result.RemovedVersions).IsEmpty();
        await Assert.That(File.Exists(_canary.ExecutablePath)).IsTrue();
        await Assert.That(File.Exists(_latest.ExecutablePath)).IsTrue();
    }

    [Test]
    [DependsOn(nameof(Cleanup_KeepsCanary))]
    public async Task UpgradeCanary_RedownloadsAndKeepsSingleCanary()
    {
        var progressReported = false;

        var runtime = await BunInstaller.UpgradeCanaryAsync(_installPath, onProgress: _ => progressReported = true);

        await Assert.That(progressReported).IsTrue();
        await Assert.That(runtime.Metadata.Version).IsEqualTo(BunVersion.Canary);
        await Assert.That(runtime.Metadata.InstalledAt).IsGreaterThan(_canary.Metadata.InstalledAt);
        await Assert.That(File.Exists(runtime.ExecutablePath)).IsTrue();

        var versions = await BunInstaller.ListVersionsAsync(_installPath);
        await Assert
            .That(versions.Select(v => v.Metadata.Version))
            .IsEquivalentTo(
                [_latest.Metadata.Version, BunVersion.Canary],
                EqualityComparer<BunVersion>.Default,
                TUnit.Assertions.Enums.CollectionOrdering.Matching
            );
    }
}
