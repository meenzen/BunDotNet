using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BunDotNet.Cli;

[Description("Upgrades Bun to the latest version.")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class UpgradeCommand : AsyncCommand<UpgradeCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class Settings : PathSettings { }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        var runtime = await ProgressBar.RunAsync(onProgress =>
            BunInstaller.UpgradeAsync(settings.Path, onProgress, cancellationToken)
        );
        AnsiConsole.MarkupLine($"[green]Bun has been upgraded to version {runtime.Metadata.Version}.[/]");
        return 0;
    }
}
