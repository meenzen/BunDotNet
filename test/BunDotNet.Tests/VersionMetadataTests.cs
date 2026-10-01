namespace BunDotNet.Tests;

public class VersionMetadataTests
{
    private static BunInstaller.VersionMetadata Metadata(BunVersion version, TimeSpan age) =>
        new()
        {
            Version = version,
            Hash = "hash",
            SizeBytes = 0,
            DownloadUrl = "https://example.com",
            Platform = "LinuxX64",
            InstalledAt = DateTimeOffset.UtcNow - age,
        };

    [Test]
    public async Task IsOutdated_FreshCanary_ReturnsFalse()
    {
        await Assert.That(Metadata(BunVersion.Canary, TimeSpan.FromHours(23)).IsOutdated()).IsFalse();
    }

    [Test]
    public async Task IsOutdated_OldCanary_ReturnsTrue()
    {
        await Assert.That(Metadata(BunVersion.Canary, TimeSpan.FromHours(25)).IsOutdated()).IsTrue();
    }

    [Test]
    public async Task IsOutdated_OldStableVersion_ReturnsFalse()
    {
        var version = BunVersion.Parse("1.3.6")!;

        await Assert.That(Metadata(version, TimeSpan.FromDays(365)).IsOutdated()).IsFalse();
    }
}
