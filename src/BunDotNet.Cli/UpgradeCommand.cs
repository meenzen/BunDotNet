using System.CommandLine;
using Spectre.Console;

namespace BunDotNet.Cli;

public static class UpgradeCommand
{
    public static Command Create()
    {
        var path = CommonOptions.Path();
        var gitHubToken = CommonOptions.GitHubToken();
        var canary = new Option<bool>("--canary")
        {
            Description = "Download the latest canary build instead of the latest stable version.",
        };
        var command = new Command("upgrade", "Upgrades Bun to the latest version, or refreshes the canary build.")
        {
            path,
            gitHubToken,
            canary,
        };
        command.SetAction(
            async (parseResult, cancellationToken) =>
            {
                var installPath = parseResult.GetValue(path);
                var isCanary = parseResult.GetValue(canary);
                var runtime = await ProgressBar.RunAsync(onProgress =>
                    isCanary switch
                    {
                        true => BunInstaller.UpgradeCanaryAsync(installPath, onProgress, cancellationToken),
                        false => BunInstaller.UpgradeAsync(
                            installPath,
                            onProgress,
                            parseResult.GetValue(gitHubToken),
                            cancellationToken
                        ),
                    }
                );
                AnsiConsole.MarkupLine(
                    isCanary switch
                    {
                        true => "[green]Bun has been upgraded to the latest canary build.[/]",
                        false => $"[green]Bun has been upgraded to version {runtime.Metadata.Version}.[/]",
                    }
                );
                return 0;
            }
        );
        return command;
    }
}
