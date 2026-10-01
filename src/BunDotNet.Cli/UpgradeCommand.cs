using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BunDotNet.Cli;

[Description("Upgrades Bun to the latest version, or refreshes the canary build.")]
public class UpgradeCommand : AsyncCommand<UpgradeCommand.Settings>
{
    public class Settings : GitHubSettings
    {
        [CommandOption("--canary")]
        [Description("Download the latest canary build instead of the latest stable version.")]
        [DefaultValue(false)]
        public bool Canary { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        var runtime = await ProgressBar.RunAsync(onProgress =>
            settings.Canary switch
            {
                true => BunInstaller.UpgradeCanaryAsync(settings.Path, onProgress, cancellationToken),
                false => BunInstaller.UpgradeAsync(settings.Path, onProgress, settings.GitHubToken, cancellationToken),
            }
        );
        AnsiConsole.MarkupLine(
            settings.Canary switch
            {
                true => "[green]Bun has been upgraded to the latest canary build.[/]",
                false => $"[green]Bun has been upgraded to version {runtime.Metadata.Version}.[/]",
            }
        );
        return 0;
    }
}
