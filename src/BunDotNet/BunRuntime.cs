using System.Diagnostics;

namespace BunDotNet;

public sealed class BunRuntime
{
    public required BunInstaller.VersionMetadata Metadata { get; init; }
    public required string ExecutablePath { get; init; }

    public static BunRuntime FromMetadata(BunInstaller.VersionMetadata metadata, BunInstallDirectory directory) =>
        new() { Metadata = metadata, ExecutablePath = directory.GetExecutablePath(metadata.Hash) };

    /// <summary>
    /// Set up a process to run Bun with the given arguments and working directory. It must be started manually.
    /// </summary>
    public Process SetupProcess(string[] args, string workingDirectory)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ExecutablePath,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            },
        };
        process.StartInfo.ArgumentList.Clear();
        foreach (var a in args)
        {
            process.StartInfo.ArgumentList.Add(a);
        }

        return process;
    }

    /// <summary>
    /// Runs Bun with the given arguments and working directory, waiting for it to complete.
    /// </summary>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <returns>The exit code of the Bun process.</returns>
    public async Task<int> RunAsync(string[] args, string workingDirectory, CancellationToken cancellationToken = default)
    {
        var process = SetupProcess(args, workingDirectory);
        process.Start();
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            process.Kill(entireProcessTree: true);
            throw new OperationCanceledException(
                "The Bun process was cancelled and has been terminated.",
                e,
                cancellationToken
            );
        }

        return process.ExitCode;
    }
}
