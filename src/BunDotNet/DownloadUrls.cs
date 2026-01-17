using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace BunDotNet;

internal enum Platform
{
    WindowsX64,
    WindowsX64Baseline,
    LinuxX64,
    LinuxX64Baseline,
    LinuxX64Musl,
    LinuxX64MuslBaseline,
    LinuxArm64,
    LinuxArm64Musl,
    MacOsX64,
    MacOsArm64,
}

internal static class PlatformExtensions
{
    extension(Platform)
    {
        internal static Platform Detect()
        {
            var isMusl = RuntimeInformation.RuntimeIdentifier.Contains("linux-musl");
            var isAvx2 = Avx2.IsSupported;

            Platform platform;
            if (
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && Environment.Is64BitOperatingSystem
                && RuntimeInformation.ProcessArchitecture == Architecture.X64
            )
            {
                platform = isAvx2 switch
                {
                    true => Platform.WindowsX64,
                    false => Platform.WindowsX64Baseline,
                };
            }
            else if (
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                && Environment.Is64BitOperatingSystem
                && RuntimeInformation.ProcessArchitecture == Architecture.X64
            )
            {
                platform = Platform.MacOsX64;
            }
            else if (
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                && Environment.Is64BitOperatingSystem
                && RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            )
            {
                platform = Platform.MacOsArm64;
            }
            else if (
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                && Environment.Is64BitOperatingSystem
                && RuntimeInformation.ProcessArchitecture == Architecture.X64
            )
            {
                platform = isMusl switch
                {
                    true => isAvx2 switch
                    {
                        true => Platform.LinuxX64Musl,
                        false => Platform.LinuxX64MuslBaseline,
                    },
                    false => isAvx2 switch
                    {
                        true => Platform.LinuxX64,
                        false => Platform.LinuxX64Baseline,
                    },
                };
            }
            else if (
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                && Environment.Is64BitOperatingSystem
                && RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            )
            {
                platform = isMusl switch
                {
                    true => Platform.LinuxArm64Musl,
                    false => Platform.LinuxArm64,
                };
            }
            else
            {
                throw new NotSupportedException("Unsupported platform");
            }

            return platform;
        }
    }
}

internal static class DownloadUrls
{
    internal static string GitHubOwner => "oven-sh";
    internal static string GitHubRepo => "bun";

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded")]
    private static readonly Dictionary<Platform, string> Filenames = new()
    {
        { Platform.WindowsX64, "bun-windows-x64.zip" },
        { Platform.WindowsX64Baseline, "bun-windows-x64-baseline.zip" },
        { Platform.LinuxX64, "bun-linux-x64.zip" },
        { Platform.LinuxX64Baseline, "bun-linux-x64-baseline.zip" },
        { Platform.LinuxX64Musl, "bun-linux-x64-musl.zip" },
        { Platform.LinuxX64MuslBaseline, "bun-linux-x64-musl-baseline.zip" },
        { Platform.LinuxArm64, "bun-linux-aarch64.zip" },
        { Platform.LinuxArm64Musl, "bun-linux-aarch64-musl.zip" },
        { Platform.MacOsX64, "bun-darwin-x64.zip" },
        { Platform.MacOsArm64, "bun-darwin-aarch64.zip" },
    };

    internal static string GetBunDownloadUrl(BunVersion version)
    {
        var platform = Platform.Detect();
        if (Filenames.TryGetValue(platform, out var filename))
        {
            var tag = version.ToGitTag();
            // example: https://github.com/oven-sh/bun/releases/download/bun-v1.3.6/bun-linux-aarch64.zip
            return $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/download/{tag}/{filename}";
        }

        throw new NotSupportedException("Missing download URL for platform. This is likely a bug.");
    }
}
