using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;

namespace BunDotNet;

public static class BunInstaller
{
    internal static readonly string ExeName = OperatingSystem.IsWindows() switch
    {
        true => "bun.exe",
        false => "bun",
    };

    public class VersionMetadata
    {
        public required BunVersion Version { get; init; }
        public required string Hash { get; init; }
        public required long SizeBytes { get; init; }
        public required string DownloadUrl { get; init; }
        public required string Platform { get; init; }
        public required DateTimeOffset InstalledAt { get; init; }

        /// <summary>
        /// Canary builds are refreshed every 24 hours. Stable versions never change and are never outdated.
        /// </summary>
        public bool IsOutdated() => Version.IsCanary && InstalledAt.AddHours(24) < DateTimeOffset.UtcNow;
    }

    public class InstallMetadata
    {
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? UpdateCheckedAt { get; set; }
        public required List<VersionMetadata> Versions { get; set; }

        public bool ShouldCheckForUpdate()
        {
            if (UpdateCheckedAt is null)
            {
                return true;
            }

            return UpdateCheckedAt.Value.AddHours(24) < DateTimeOffset.UtcNow;
        }
    }

    private static async Task SaveMetadataAsync(BunInstallDirectory directory, InstallMetadata metadata)
    {
        var metadataPath = directory.GetMetadataJsonPath();
        metadata.Versions = metadata.Versions.OrderBy(v => v.Version).ToList();
        var json = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(metadataPath, json);
    }

    private static async Task<InstallMetadata> LoadMetadataAsync(
        BunInstallDirectory directory,
        CancellationToken cancellationToken
    )
    {
        var metadataPath = directory.GetMetadataJsonPath();
        if (!File.Exists(metadataPath))
        {
            var metadata = new InstallMetadata
            {
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Versions = [],
            };
            await SaveMetadataAsync(directory, metadata);
            return metadata;
        }

        var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
        return JsonSerializer.Deserialize<InstallMetadata>(json)!;
    }

    private static async Task<BunVersion> GetLatestVersionAsync(
        BunInstallDirectory directory,
        string? gitHubToken,
        CancellationToken cancellationToken
    )
    {
        var token = await GitHubToken.ResolveAsync(gitHubToken, cancellationToken: cancellationToken);
        using var gitHub = new GitHubClient(token);
        var tag = await gitHub.GetLatestReleaseTagAsync(
            DownloadUrls.GitHubOwner,
            DownloadUrls.GitHubRepo,
            cancellationToken
        );
        var version = BunVersion.Parse(tag)!;

        // save update checked at timestamp
        using var @lock = InstallLock.Acquire(directory);
        var metadata = await LoadMetadataAsync(directory, cancellationToken);
        metadata.UpdateCheckedAt = DateTimeOffset.UtcNow;
        await SaveMetadataAsync(directory, metadata);

        return version;
    }

    public record DownloadProgress(long Read, long? Total);

    private static async Task DownloadWithProgressAsync(
        string url,
        Stream destinationStream,
        Action<DownloadProgress>? onProgress,
        CancellationToken cancellationToken
    )
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[81920];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            onProgress?.Invoke(new DownloadProgress(totalBytesRead, totalBytes));
        }

        destinationStream.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// Important: The caller must acquire the installation lock before calling this method.
    /// </summary>
    /// <param name="version">The version to install.</param>
    /// <param name="directory">The installation directory.</param>
    /// <param name="onProgress">A callback to report download progress.</param>
    /// <param name="force">Download the version even if it is already installed. Used to refresh canary builds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    private static async Task<BunRuntime> DownloadAndInstallAsync(
        BunVersion version,
        BunInstallDirectory directory,
        Action<DownloadProgress>? onProgress = null,
        bool force = false,
        CancellationToken cancellationToken = default
    )
    {
        // Check if the requested version was installed (or refreshed) while waiting for the lock
        var metadata = await LoadMetadataAsync(directory, cancellationToken);
        var existingVersion = metadata.Versions.FirstOrDefault(v => v.Version == version);
        if (existingVersion is not null && !force && !existingVersion.IsOutdated())
        {
            return BunRuntime.FromMetadata(existingVersion, directory);
        }

        // Download Bun archive
        var url = DownloadUrls.GetBunDownloadUrl(version);
        await using var archiveStream = new MemoryStream();
        await DownloadWithProgressAsync(url, archiveStream, onProgress, cancellationToken);

        // Find bun executable in the archive
        await using var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        var exeEntry = zipArchive.Entries.FirstOrDefault(e => e.Name == ExeName);
        if (exeEntry is null)
        {
            throw new FileNotFoundException(
                "Bun executable not found in the downloaded zip file. Did the format change?"
            );
        }

        // Extract bun executable
        await using var exeStream = await exeEntry.OpenAsync(cancellationToken);
        await using var exeMemoryStream = new MemoryStream();
        await exeStream.CopyToAsync(exeMemoryStream, cancellationToken);
        var exeBytes = exeMemoryStream.ToArray();
        var hash = exeBytes.Hash();
        var path = directory.GetVersionDirectory(hash);
        var filename = directory.GetExecutablePath(hash);

        // A refreshed canary build may be identical to the installed one. Skip writing it, the executable may be
        // running and cannot be overwritten.
        if (existingVersion?.Hash != hash || !File.Exists(filename))
        {
            Directory.CreateDirectory(path);
            await File.WriteAllBytesAsync(filename, exeBytes, CancellationToken.None);
            if (!OperatingSystem.IsWindows())
            {
                // Make executable
                var fileInfo = new FileInfo(filename);
                fileInfo.UnixFileMode |=
                    UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            }
        }

        // Replace a previously installed build of the same version, which happens when refreshing canary
        if (existingVersion is not null)
        {
            metadata.Versions.Remove(existingVersion);
        }

        // Write metadata
        var versionMetadata = new VersionMetadata
        {
            Version = version,
            Hash = hash,
            SizeBytes = exeBytes.Length,
            DownloadUrl = url,
            Platform = Platform.Detect().ToString(),
            InstalledAt = DateTimeOffset.UtcNow,
        };
        metadata.Versions.Add(versionMetadata);
        metadata.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveMetadataAsync(directory, metadata);

        // Only delete the previous build once the metadata no longer references it. If saving fails, the previous
        // build stays usable.
        if (existingVersion is not null && existingVersion.Hash != hash)
        {
            TryDeleteVersionDirectory(directory, existingVersion.Hash);
        }

        return new BunRuntime { Metadata = versionMetadata, ExecutablePath = filename };
    }

    [SuppressMessage("Roslynator", "RCS1075:Avoid empty catch clause that catches System.Exception")]
    private static void TryDeleteVersionDirectory(BunInstallDirectory directory, string hash)
    {
        try
        {
            var versionDirectory = directory.GetVersionDirectory(hash);
            if (Directory.Exists(versionDirectory))
            {
                Directory.Delete(versionDirectory, recursive: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The old build may still be running (Windows locks running executables). It is no longer referenced by
            // the metadata, so leaving it behind is harmless.
        }
    }

    /// <summary>
    /// Installs the Bun runtime.
    /// </summary>
    /// <param name="version">
    /// The version of Bun to install. If null, the latest stable version will be used. Use <see cref="BunVersion.Canary"/>
    /// for the latest canary build.
    /// </param>
    /// <param name="path">The path to install Bun to. If null, the default installation path will be used.</param>
    /// <param name="onProgress">A callback to report download progress.</param>
    /// <param name="gitHubToken">
    /// The token used to query the latest version from the GitHub API. If null, a token is discovered automatically
    /// from the environment or the GitHub CLI, see the README for details.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The installed Bun runtime.</returns>
    /// <remarks>
    /// This is idempotent, calling this method multiple times with the same arguments will not reinstall Bun. The only
    /// exception is <see cref="BunVersion.Canary"/>, which is refreshed if the installed build is older than 24 hours.
    /// </remarks>
    [SuppressMessage("Roslynator", "RCS1075:Avoid empty catch clause that catches System.Exception")]
    public static async Task<BunRuntime> InstallAsync(
        BunVersion? version = null,
        string? path = null,
        Action<DownloadProgress>? onProgress = null,
        string? gitHubToken = null,
        CancellationToken cancellationToken = default
    )
    {
        var directory = BunInstallDirectory.Parse(path);
        var metadata = await LoadMetadataAsync(directory, cancellationToken);

        // If no version is specified, check for updates
        if (version is null && metadata.ShouldCheckForUpdate())
        {
            try
            {
                version = await GetLatestVersionAsync(directory, gitHubToken, cancellationToken);
            }
            catch (Exception)
            {
                // Ignore errors during update check. This allows offline usage if a version is already installed.
            }
        }

        // If no version is specified and there are existing stable versions, use the latest installed
        var latestInstalled = metadata
            .Versions.Where(v => !v.Version.IsCanary)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();
        if (version is null && latestInstalled is not null)
        {
            return BunRuntime.FromMetadata(latestInstalled, directory);
        }

        // If no version is specified, fetch the latest version
        version ??= await GetLatestVersionAsync(directory, gitHubToken, cancellationToken);

        // Check if the requested version is already installed
        var existingVersion = metadata.Versions.FirstOrDefault(v => v.Version == version);
        if (existingVersion is not null && !existingVersion.IsOutdated())
        {
            return BunRuntime.FromMetadata(existingVersion, directory);
        }

        // Try to install the requested version
        using var @lock = InstallLock.Acquire(directory);
        if (existingVersion is null)
        {
            return await DownloadAndInstallAsync(version, directory, onProgress, cancellationToken: cancellationToken);
        }

        // Refresh an outdated canary build
        try
        {
            return await DownloadAndInstallAsync(version, directory, onProgress, cancellationToken: cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Fall back to the installed build. This allows offline usage.
            return BunRuntime.FromMetadata(existingVersion, directory);
        }
    }

    /// <summary>
    /// Upgrades Bun to the latest stable version.
    /// </summary>
    /// <param name="path">The path to the Bun installation. If null, the default installation path will be used.</param>
    /// <param name="onProgress">A callback to report download progress.</param>
    /// <param name="gitHubToken">
    /// The token used to query the latest version from the GitHub API. If null, a token is discovered automatically
    /// from the environment or the GitHub CLI, see the README for details.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task<BunRuntime> UpgradeAsync(
        string? path = null,
        Action<DownloadProgress>? onProgress = null,
        string? gitHubToken = null,
        CancellationToken cancellationToken = default
    )
    {
        var directory = BunInstallDirectory.Parse(path);
        var latestVersion = await GetLatestVersionAsync(directory, gitHubToken, cancellationToken);
        return await InstallAsync(latestVersion, path, onProgress, gitHubToken, cancellationToken);
    }

    /// <summary>
    /// Downloads the latest canary build of Bun, replacing the installed canary build regardless of its age.
    /// </summary>
    /// <param name="path">The path to the Bun installation. If null, the default installation path will be used.</param>
    /// <param name="onProgress">A callback to report download progress.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task<BunRuntime> UpgradeCanaryAsync(
        string? path = null,
        Action<DownloadProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        var directory = BunInstallDirectory.Parse(path);
        using var @lock = InstallLock.Acquire(directory);
        return await DownloadAndInstallAsync(BunVersion.Canary, directory, onProgress, force: true, cancellationToken);
    }

    /// <summary>
    /// Lists all installed Bun versions.
    /// </summary>
    /// <param name="path">The path to the Bun installation. If null, the default installation path will be used.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task<IReadOnlyList<BunRuntime>> ListVersionsAsync(
        string? path = null,
        CancellationToken cancellationToken = default
    )
    {
        var directory = BunInstallDirectory.Parse(path);
        var metadata = await LoadMetadataAsync(directory, cancellationToken);
        return metadata.Versions.ConvertAll(version => BunRuntime.FromMetadata(version, directory));
    }

    /// <summary>
    /// Removes all Bun versions except the latest stable one and the canary build.
    /// </summary>
    /// <param name="path">The path to the Bun installation. If null, the default installation path will be used.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task<BunCleanupResult> CleanupAsync(
        string? path = null,
        CancellationToken cancellationToken = default
    )
    {
        var directory = BunInstallDirectory.Parse(path);
        var metadata = await LoadMetadataAsync(directory, cancellationToken);

        if (GetVersionsToRemove(metadata).Count == 0)
        {
            return new BunCleanupResult { RemovedVersions = [] };
        }

        using var @lock = InstallLock.Acquire(directory);
        // double-check after acquiring the lock
        metadata = await LoadMetadataAsync(directory, cancellationToken);
        var versionsToRemove = GetVersionsToRemove(metadata);
        if (versionsToRemove.Count == 0)
        {
            return new BunCleanupResult { RemovedVersions = [] };
        }

        foreach (var version in versionsToRemove)
        {
            var versionDirectory = directory.GetVersionDirectory(version.Hash);
            if (Directory.Exists(versionDirectory))
            {
                Directory.Delete(versionDirectory, recursive: true);
            }
        }

        metadata.Versions = metadata.Versions.Except(versionsToRemove).ToList();
        metadata.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveMetadataAsync(directory, metadata);

        return new BunCleanupResult
        {
            RemovedVersions = versionsToRemove.ConvertAll(version => BunRuntime.FromMetadata(version, directory)),
        };
    }

    // all stable versions except the latest one, canary is kept
    private static List<VersionMetadata> GetVersionsToRemove(InstallMetadata metadata) =>
        metadata
            .Versions.Where(version => !version.Version.IsCanary)
            .OrderByDescending(version => version.Version)
            .Skip(1)
            .ToList();
}
