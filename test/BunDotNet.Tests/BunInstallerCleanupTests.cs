using System.Text.Json;

namespace BunDotNet.Tests;

public class BunInstallerCleanupTests
{
    private static BunInstallDirectory CreateDirectory() =>
        new(Path.Combine(Path.GetTempPath(), "BunDotNet.Tests", Guid.NewGuid().ToString("N")));

    private static BunInstaller.VersionMetadata Version(string version, string hash) =>
        new()
        {
            Version = BunVersion.Parse(version)!,
            Hash = hash,
            SizeBytes = 0,
            DownloadUrl = "https://example.com",
            Platform = "LinuxX64",
            InstalledAt = DateTimeOffset.UtcNow,
        };

    // writes the metadata and a fake executable for every version, without downloading anything
    private static async Task InstallFakeVersionsAsync(
        BunInstallDirectory directory,
        params BunInstaller.VersionMetadata[] versions
    )
    {
        foreach (var version in versions)
        {
            Directory.CreateDirectory(directory.GetVersionDirectory(version.Hash));
            await File.WriteAllTextAsync(directory.GetExecutablePath(version.Hash), version.Hash);
        }

        var metadata = new BunInstaller.InstallMetadata
        {
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Versions = [.. versions],
        };
        await File.WriteAllTextAsync(directory.GetMetadataJsonPath(), JsonSerializer.Serialize(metadata));
    }

    [Test]
    public async Task Cleanup_RemovedVersionSharesHashWithLatest_KeepsExecutable()
    {
        var directory = CreateDirectory();
        await InstallFakeVersionsAsync(directory, Version("1.3.5", "shared"), Version("1.3.6", "shared"));

        var result = await BunInstaller.CleanupAsync(directory.Base);

        await Assert.That(result.RemovedVersions.Count).IsEqualTo(1);
        await Assert.That(result.RemovedVersions[0].Metadata.Version).IsEqualTo(BunVersion.Parse("1.3.5"));
        await Assert.That(File.Exists(directory.GetExecutablePath("shared"))).IsTrue();
        Directory.Delete(directory.Base, recursive: true);
    }

    [Test]
    public async Task Cleanup_RemovedVersionSharesHashWithCanary_KeepsExecutable()
    {
        var directory = CreateDirectory();
        await InstallFakeVersionsAsync(
            directory,
            Version("1.3.4", "old"),
            Version("1.3.5", "canary-build"),
            Version("1.3.6", "latest"),
            Version("canary", "canary-build")
        );

        var result = await BunInstaller.CleanupAsync(directory.Base);

        await Assert
            .That(result.RemovedVersions.Select(v => v.Metadata.Version.ToString()))
            .IsEquivalentTo(["1.3.5", "1.3.4"]);
        await Assert.That(File.Exists(directory.GetExecutablePath("old"))).IsFalse();
        await Assert.That(File.Exists(directory.GetExecutablePath("canary-build"))).IsTrue();
        await Assert.That(File.Exists(directory.GetExecutablePath("latest"))).IsTrue();

        var versions = await BunInstaller.ListVersionsAsync(directory.Base);
        await Assert
            .That(versions.Select(v => v.Metadata.Version))
            .IsEquivalentTo(
                [BunVersion.Parse("1.3.6")!, BunVersion.Canary],
                TUnit.Assertions.Enums.CollectionOrdering.Matching
            );
        Directory.Delete(directory.Base, recursive: true);
    }

    [Test]
    public async Task Cleanup_UnsharedOldVersions_DeletesExecutables()
    {
        var directory = CreateDirectory();
        await InstallFakeVersionsAsync(directory, Version("1.3.5", "a"), Version("1.3.6", "b"));

        await BunInstaller.CleanupAsync(directory.Base);

        await Assert.That(File.Exists(directory.GetExecutablePath("a"))).IsFalse();
        await Assert.That(File.Exists(directory.GetExecutablePath("b"))).IsTrue();
        Directory.Delete(directory.Base, recursive: true);
    }

    private static BunInstaller.InstallMetadata Metadata(params BunInstaller.VersionMetadata[] retained) =>
        new()
        {
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Versions = [.. retained],
        };

    [Test]
    public async Task GetUnreferencedHashes_CanaryRefreshedToDifferentBuild_ReturnsOldHash()
    {
        var metadata = Metadata(Version("1.3.6", "stable"), Version("canary", "new"));

        var hashes = BunInstaller.GetUnreferencedHashes(metadata, [Version("canary", "old")]);

        await Assert.That(hashes).IsEquivalentTo(["old"]);
    }

    [Test]
    public async Task GetUnreferencedHashes_CanaryRefreshedToSameBuild_ReturnsNothing()
    {
        var metadata = Metadata(Version("canary", "same"));

        var hashes = BunInstaller.GetUnreferencedHashes(metadata, [Version("canary", "same")]);

        await Assert.That(hashes).IsEmpty();
    }

    [Test]
    public async Task GetUnreferencedHashes_OldCanarySharesHashWithStable_ReturnsNothing()
    {
        var metadata = Metadata(Version("1.3.6", "shared"), Version("canary", "new"));

        var hashes = BunInstaller.GetUnreferencedHashes(metadata, [Version("canary", "shared")]);

        await Assert.That(hashes).IsEmpty();
    }

    [Test]
    public async Task GetUnreferencedHashes_RemovedVersionsShareHash_ReturnsHashOnce()
    {
        var metadata = Metadata(Version("1.3.6", "latest"));

        var hashes = BunInstaller.GetUnreferencedHashes(
            metadata,
            [Version("1.3.4", "duplicate"), Version("1.3.5", "duplicate")]
        );

        await Assert.That(hashes).IsEquivalentTo(["duplicate"]);
    }
}
