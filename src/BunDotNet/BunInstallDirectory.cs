namespace BunDotNet;

public class BunInstallDirectory
{
    /// <summary>
    /// The directory where bun is installed.
    /// </summary>
    /// <param name="base">The base directory for bun installations. A subdirectory "BunDotNet" will be created within this directory.</param>
    public BunInstallDirectory(string @base)
    {
        Base = @base;
        Directory.CreateDirectory(Base);
        Full = Path.Combine(Base, "BunDotNet");
        Directory.CreateDirectory(Full);
    }

    public static BunInstallDirectory Default =>
        new(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static BunInstallDirectory Parse(string? path) =>
        string.IsNullOrWhiteSpace(path) switch
        {
            true => Default,
            false => new BunInstallDirectory(path),
        };

    public string Base { get; }

    public string Full { get; }
}

public static class BunInstallDirectoryExtensions
{
    extension(BunInstallDirectory installDirectory)
    {
        public string GetVersionDirectory(string hash) => Path.Combine(installDirectory.Full, hash);

        public string GetExecutablePath(string hash)
        {
            var versionDirectory = installDirectory.GetVersionDirectory(hash);
            var executableName = BunInstaller.ExeName;
            return Path.Combine(versionDirectory, executableName);
        }

        public string GetMetadataJsonPath() => Path.Combine(installDirectory.Full, "metadata.json");
    }
}
